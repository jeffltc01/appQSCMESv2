import { readdir, readFile, writeFile, mkdir } from 'node:fs/promises';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { mdToPdf } from 'md-to-pdf';

const __dirname = dirname(fileURLToPath(import.meta.url));
const articlesDir = join(__dirname, '..', 'src', 'help', 'articles');
const outDir = join(__dirname, '..', 'public', 'help');
const outPath = join(outDir, 'MESv2-Help-Manual.pdf');

const articleOrder = [
  // General
  'overview',
  'login',
  'tablet-setup',
  'menu',
  'barcode-reference',
  // Operator
  'operator-layout',
  'rolls',
  'rolls-material',
  'long-seam',
  'long-seam-insp',
  'fitup',
  'fitup-queue',
  'round-seam',
  'round-seam-insp',
  'rt-xray-queue',
  'spot-xray',
  'nameplate',
  'hydro',
  // Admin
  'products',
  'users',
  'vendors',
  'work-centers',
  'defect-codes',
  'defect-locations',
  'assets',
  'kanban-cards',
  'characteristics',
  'control-plans',
  'plant-gear',
  'whos-on-floor',
  'production-lines',
  'annotation-types',
  'annotations',
  'serial-lookup',
  'sellable-tank-status',
  'plant-printers',
  'report-issue',
  'issue-approvals',
  'ai-review',
  'production-logs',
  'supervisor-dashboard',
  'downtime-reasons',
  'digital-twin',
  'shift-schedule',
  'capacity-targets',
];

async function getAvailableArticles() {
  const files = await readdir(articlesDir);
  const mdFiles = files.filter((f) => f.endsWith('.md'));
  return mdFiles.map((f) => f.replace('.md', ''));
}

function buildCoverPage() {
  const date = new Date().toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
  return [
    '<div style="text-align: center; padding-top: 200px;">',
    '<h1 style="font-size: 36px; color: #2b3b84; margin-bottom: 8px;">MES v2 Help Manual</h1>',
    `<p style="font-size: 16px; color: #868686;">Quality Steel Corporation</p>`,
    `<p style="font-size: 14px; color: #868686;">Generated ${date}</p>`,
    '</div>',
    '',
    '<div style="page-break-after: always;"></div>',
    '',
  ].join('\n');
}

function buildTableOfContents(slugs) {
  const lines = ['# Table of Contents', ''];
  let sectionHeader = '';
  for (const slug of slugs) {
    const idx = articleOrder.indexOf(slug);
    if (idx < 5 && sectionHeader !== 'General') {
      sectionHeader = 'General';
      lines.push(`## ${sectionHeader}`, '');
    } else if (idx >= 5 && idx < 18 && sectionHeader !== 'Operator Screens') {
      sectionHeader = 'Operator Screens';
      lines.push(`## ${sectionHeader}`, '');
    } else if (idx >= 18 && sectionHeader !== 'Admin & Supervisor') {
      sectionHeader = 'Admin & Supervisor';
      lines.push(`## ${sectionHeader}`, '');
    }
    const title = slug
      .split('-')
      .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
      .join(' ');
    lines.push(`- ${title}`);
  }
  lines.push('', '<div style="page-break-after: always;"></div>', '');
  return lines.join('\n');
}

async function main() {
  const available = await getAvailableArticles();
  const orderedSlugs = articleOrder.filter((s) => available.includes(s));
  const extras = available.filter((s) => !articleOrder.includes(s));
  const allSlugs = [...orderedSlugs, ...extras];

  if (allSlugs.length === 0) {
    console.error('No markdown articles found in', articlesDir);
    process.exit(1);
  }

  console.log(`Building PDF from ${allSlugs.length} articles...`);

  const parts = [buildCoverPage(), buildTableOfContents(allSlugs)];

  for (const slug of allSlugs) {
    const filePath = join(articlesDir, `${slug}.md`);
    const content = await readFile(filePath, 'utf-8');
    parts.push(content);
    parts.push('\n<div style="page-break-after: always;"></div>\n');
  }

  const combined = parts.join('\n');

  await mkdir(outDir, { recursive: true });

  const pdf = await mdToPdf(
    { content: combined },
    {
      stylesheet: [],
      css: `
        body {
          font-family: 'Roboto', 'Helvetica', 'Arial', sans-serif;
          font-size: 12px;
          line-height: 1.6;
          color: #212529;
        }
        h1 {
          color: #2b3b84;
          font-size: 22px;
          border-bottom: 2px solid #2b3b84;
          padding-bottom: 4px;
          margin-top: 0;
        }
        h2 {
          color: #212529;
          font-size: 16px;
          margin-top: 20px;
        }
        h3 {
          color: #343a40;
          font-size: 14px;
          margin-top: 14px;
        }
        table {
          width: 100%;
          border-collapse: collapse;
          margin: 12px 0;
          font-size: 11px;
        }
        th {
          background: #f0f0f0;
          font-weight: 600;
          text-align: left;
          padding: 6px 8px;
          border: 1px solid #dfe2ed;
        }
        td {
          padding: 4px 8px;
          border: 1px solid #dfe2ed;
          vertical-align: top;
        }
        code {
          font-family: 'Consolas', 'Courier New', monospace;
          font-size: 10px;
          background: #f0f0f0;
          padding: 1px 3px;
        }
        pre {
          background: #f0f0f0;
          padding: 10px;
          font-size: 10px;
          overflow-x: auto;
        }
      `,
      pdf_options: {
        format: 'Letter',
        margin: { top: '0.75in', bottom: '0.75in', left: '0.75in', right: '0.75in' },
        printBackground: true,
        displayHeaderFooter: true,
        headerTemplate: '<div></div>',
        footerTemplate:
          '<div style="width: 100%; text-align: center; font-size: 9px; color: #868686; font-family: Roboto, Helvetica, Arial, sans-serif;">' +
          'MES v2 Help Manual &mdash; Quality Steel Corporation &mdash; Page <span class="pageNumber"></span> of <span class="totalPages"></span>' +
          '</div>',
      },
    },
  );

  if (pdf.content) {
    await writeFile(outPath, pdf.content);
    console.log(`PDF written to ${outPath} (${(pdf.content.length / 1024).toFixed(0)} KB)`);
  } else {
    console.error('PDF generation failed — no content returned.');
    process.exit(1);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
