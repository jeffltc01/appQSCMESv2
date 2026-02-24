import { mkdir, readFile, writeFile, access } from 'node:fs/promises';
import { constants as fsConstants } from 'node:fs';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import ffmpegStatic from 'ffmpeg-static';

const ROOT = process.cwd();
const TRAINING_DIR = join(ROOT, 'training', 'rolls');
const TIMELINE_PATH = join(TRAINING_DIR, 'rolls-video-timeline.json');
const SCRIPT_PATH = join(TRAINING_DIR, 'rolls-training-script.md');
const SCREENSHOT_DIR = join(ROOT, 'artifacts', 'rolls-training', 'screenshots');
const BUILD_DIR = join(ROOT, 'artifacts', 'rolls-training', 'build');
const OUTPUT_DIR = join(ROOT, 'artifacts', 'rolls-training');

function toSrtTime(totalSeconds) {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = Math.floor(totalSeconds % 60);
  const millis = Math.round((totalSeconds - Math.floor(totalSeconds)) * 1000);
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')},${String(millis).padStart(3, '0')}`;
}

function run(command, args) {
  const result = spawnSync(command, args, {
    cwd: ROOT,
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });
  if (result.status !== 0) {
    throw new Error(`Command failed: ${command} ${args.join(' ')}`);
  }
}

function runCapture(command, args) {
  const result = spawnSync(command, args, {
    cwd: ROOT,
    stdio: ['ignore', 'pipe', 'pipe'],
    shell: process.platform === 'win32',
  });
  if (result.status !== 0) {
    throw new Error(`Command failed: ${command} ${args.join(' ')}\n${result.stderr?.toString() ?? ''}`);
  }
  return (result.stdout?.toString() ?? '').trim();
}

function resolveFfmpegPath() {
  if (process.env.FFMPEG_PATH) return process.env.FFMPEG_PATH;
  if (ffmpegStatic) return ffmpegStatic;
  return 'ffmpeg';
}

async function assertFileExists(path) {
  await access(path, fsConstants.F_OK);
}

function synthesizeNarrationWav(text, outPath) {
  const textB64 = Buffer.from(text, 'utf8').toString('base64');
  const outEscaped = outPath.replaceAll("'", "''");
  const psScript = [
    'Add-Type -AssemblyName System.Speech',
    `$bytes = [Convert]::FromBase64String('${textB64}')`,
    '$text = [System.Text.Encoding]::UTF8.GetString($bytes)',
    '$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer',
    '$synth.Rate = 0',
    '$synth.Volume = 100',
    "try { $synth.SelectVoice('Microsoft Zira Desktop') } catch {}",
    `$synth.SetOutputToWaveFile('${outEscaped}')`,
    '$synth.Speak($text)',
    '$synth.Dispose()',
  ].join('; ');

  run('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', psScript]);
}

async function main() {
  const ffmpegCmd = resolveFfmpegPath();
  const ffmpegCheck = spawnSync(ffmpegCmd, ['-version'], {
    stdio: 'ignore',
    shell: process.platform === 'win32',
  });
  if (ffmpegCheck.status !== 0) {
    throw new Error(`ffmpeg is required but unavailable at: ${ffmpegCmd}`);
  }

  const rawTimeline = await readFile(TIMELINE_PATH, 'utf8');
  const timeline = JSON.parse(rawTimeline);
  const scenes = timeline.scenes ?? [];
  if (scenes.length === 0) {
    throw new Error('Timeline has no scenes.');
  }

  await mkdir(BUILD_DIR, { recursive: true });
  await mkdir(OUTPUT_DIR, { recursive: true });

  for (const scene of scenes) {
    const shotPath = join(SCREENSHOT_DIR, scene.screenshot);
    await assertFileExists(shotPath);
  }

  const concatPath = join(BUILD_DIR, 'rolls-training-concat.txt');
  const captionsPath = join(BUILD_DIR, 'rolls-training-captions.srt');
  const narrationPath = join(BUILD_DIR, 'rolls-training-narration.txt');
  const voiceoverPath = join(BUILD_DIR, 'rolls-training-voiceover.wav');
  const tempVideoPath = join(BUILD_DIR, 'rolls-training-video-temp.mp4');
  const outputPath = join(OUTPUT_DIR, timeline.outputFileName || 'rolls-training-video.mp4');

  const concatLines = [];
  for (const scene of scenes) {
    concatLines.push(`file '${join(SCREENSHOT_DIR, scene.screenshot).replaceAll('\\', '/')}'`);
    concatLines.push(`duration ${scene.durationSec}`);
  }
  const lastShot = scenes[scenes.length - 1];
  concatLines.push(`file '${join(SCREENSHOT_DIR, lastShot.screenshot).replaceAll('\\', '/')}'`);
  await writeFile(concatPath, `${concatLines.join('\n')}\n`, 'utf8');

  let current = 0;
  const srtLines = [];
  const narrationLines = [`# ${timeline.title}`, ''];
  scenes.forEach((scene, idx) => {
    const start = current;
    const end = current + scene.durationSec;
    current = end;
    srtLines.push(String(idx + 1));
    srtLines.push(`${toSrtTime(start)} --> ${toSrtTime(end)}`);
    srtLines.push(scene.caption || '');
    srtLines.push('');
    narrationLines.push(`${idx + 1}. ${scene.narration || ''}`);
  });
  await writeFile(captionsPath, `${srtLines.join('\n')}\n`, 'utf8');
  await writeFile(narrationPath, `${narrationLines.join('\n')}\n`, 'utf8');

  // Generate scene-by-scene voiceover using Windows SpeechSynthesizer, then align
  // each clip to its scene duration so audio and visuals stay synchronized.
  const audioConcatPath = join(BUILD_DIR, 'rolls-training-audio-concat.txt');
  const audioConcatLines = [];
  for (let idx = 0; idx < scenes.length; idx += 1) {
    const scene = scenes[idx];
    const rawAudioPath = join(BUILD_DIR, `scene-${String(idx + 1).padStart(2, '0')}-raw.wav`);
    const alignedAudioPath = join(BUILD_DIR, `scene-${String(idx + 1).padStart(2, '0')}-aligned.wav`);
    synthesizeNarrationWav(scene.narration || scene.caption || '', rawAudioPath);

    // Pad shorter speech with silence and trim longer speech to scene duration.
    run(ffmpegCmd, [
      '-y',
      '-i', rawAudioPath,
      '-af', `apad=pad_dur=${scene.durationSec},atrim=0:${scene.durationSec}`,
      '-ar', '48000',
      '-ac', '2',
      alignedAudioPath,
    ]);
    audioConcatLines.push(`file '${alignedAudioPath.replaceAll('\\', '/')}'`);
  }
  await writeFile(audioConcatPath, `${audioConcatLines.join('\n')}\n`, 'utf8');

  run(ffmpegCmd, [
    '-y',
    '-f', 'concat',
    '-safe', '0',
    '-i', audioConcatPath,
    '-c:a', 'pcm_s16le',
    voiceoverPath,
  ]);

  // Primary render from screenshot timeline.
  run(ffmpegCmd, [
    '-y',
    '-f', 'concat',
    '-safe', '0',
    '-i', concatPath,
    '-vf', 'scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:black,fps=30,format=yuv420p',
    '-c:v', 'libx264',
    '-preset', 'medium',
    '-crf', '20',
    '-movflags', '+faststart',
    tempVideoPath,
  ]);

  // Add captions as a selectable subtitle track (mov_text).
  run(ffmpegCmd, [
    '-y',
    '-i', tempVideoPath,
    '-i', voiceoverPath,
    '-i', captionsPath,
    '-map', '0:v:0',
    '-map', '1:a:0',
    '-map', '2:0',
    '-c:v', 'copy',
    '-c:a', 'aac',
    '-b:a', '192k',
    '-c:s', 'mov_text',
    '-metadata:s:a:0', 'language=eng',
    '-metadata:s:s:0', 'language=eng',
    outputPath,
  ]);

  // Keep a copy of the authored script beside build assets.
  await assertFileExists(SCRIPT_PATH);
  const scriptMd = await readFile(SCRIPT_PATH, 'utf8');
  await writeFile(join(BUILD_DIR, 'rolls-training-script-copy.md'), scriptMd, 'utf8');

  console.log(`Video created at: ${outputPath}`);
  console.log(`Voiceover audio: ${voiceoverPath}`);
  console.log(`Captions file: ${captionsPath}`);
  console.log(`Narration text: ${narrationPath}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
