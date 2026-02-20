# Quality Steel Corporation — Web Style Guide

> **Source**: [qualitysteelcorporation.com](https://qualitysteelcorporation.com/)
> **Date reviewed**: February 19, 2026
> **Theme platform**: WordPress (BeTheme v27.6.1 parent, "Quality-Steel" child theme)
> **CSS framework**: Bootstrap 4.5.2 (customized)
> **Purpose**: Provide definitive visual and UX guidance for any application built under the Quality Steel / LT Corporation brand family.

---

## 1. Color Palette

### 1.1 Brand Colors (CSS custom properties)

| Role | Token | Hex | Usage |
|---|---|---|---|
| **Primary** | `--primary` / `--blue` | `#2b3b84` | Headers, nav icons, links, primary buttons, hero overlays, form submit buttons |
| **Secondary** | `--secondary` | `#e41e2f` | Sub-menus, accent buttons (CTA "Click Here"), red button variant |
| **Waikawa (tertiary)** | `--theme-waikawa` | `#606ca3` | Third-tier backgrounds, muted accents |
| **Dark Red** | `--theme-red` | `#aa121f` | Sixth-tier backgrounds, deep-red emphasis |

### 1.2 Neutral Colors

| Role | Token | Hex | Usage |
|---|---|---|---|
| **Body text** | — | `#212529` | Default paragraph text |
| **Dark** | `--dark` | `#343a40` | Dark UI surfaces |
| **Gray** | `--theme-gray` / `--gray` | `#868686` / `#6c757d` | Muted text, sub-menu hover text |
| **Light Gray** | `--theme-light-gray` | `#dfe2ed` | Fourth-tier backgrounds, subtle fills |
| **Light** | `--light` | `#f8f9fa` | Page backgrounds, light surfaces |
| **White** | `--white` | `#ffffff` | Backgrounds, text on dark surfaces, card backgrounds |
| **Black** | `--black` | `#000000` | Rare; shadows, deep emphasis |

### 1.3 Functional / State Colors

| Role | Hex | Usage |
|---|---|---|
| **Success** | `#28a745` | Positive states |
| **Warning** | `#ffc107` | Caution states |
| **Danger** | `#dc3545` | Error states |
| **Info** | `#17a2b8` | Informational states |

### 1.4 Overlay & Transparency Values

| Context | Value |
|---|---|
| Hero dark overlay | `rgba(0, 0, 0, 0.25)` |
| Hero blue bar overlay | `rgba(43, 59, 132, 0.78)` with `mix-blend-mode: multiply` |
| Footer link default | `rgba(255, 255, 255, 0.25)` |
| Footer link hover | `#ffffff` |
| Footer social icon bg | `rgba(255, 255, 255, 0.25)` |

### 1.5 Background Classes Reference

| Class | Background | Text |
|---|---|---|
| `.primary-background` | `#2b3b84` | `#fff` |
| `.secondary-background` | `#e41e2f` | `#fff` |
| `.third-background` | `#606ca3` | — |
| `.fourth-background` | `#dfe2ed` | — |
| `.fifth-background` | `#868686` | — |
| `.sixth-background` | `#aa121f` | — |

---

## 2. Typography

### 2.1 Font Stack

```
--font-family-sans-serif: "Roboto", Helvetica, Arial, sans-serif;
--font-family-monospace: SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
```

**Google Fonts loaded**: `Roboto` at weights 300 (light/italic), 400 (regular), 500 (medium), 700 (bold), 900 (black).

### 2.2 Type Scale

| Element | Size | Weight | Line-Height | Notes |
|---|---|---|---|---|
| **Body** | `1rem` (16px) | 400 | 1.5 | Default text |
| **H1 (hero)** | Large (browser default scaled) | 900 (Black) | — | White, text-shadow, `max-width: 800px` |
| **H2** | — | — | — | Used for section titles ("Steel Tanks", "Life at Quality Steel") |
| **H3** | — | — | — | Module headings ("Our Quality Commitment", "Our Customer Service") |
| **H4** | — | — | — | Footer widget headings, sub-section headers |
| **H6 (subtitle)** | — | — | — | Used as subtitle/tagline beneath H1 in heroes and slider |
| **Nav items** | `1.2rem` | 300 (Light) | `20px` | Menu links |
| **Footer links** | `18px` | — | `1` | `rgba(255,255,255,0.25)` default, white on hover |
| **Button labels** | — | 600 (Semi-bold) | `20px` | General buttons |
| **Small (mobile button)** | `12px` | — | `15px` | Mobile-specific override |

### 2.3 Heading Color Patterns

- **Primary-color headings**: `#2b3b84` — applied via `.primary-color` on headings (`h1`–`h6`, `p`, `li`)
- **White text headings**: `#ffffff` — applied via `.white-text` on dark backgrounds
- **Title elements**: `#2b3b84` (forced via `!important`)

### 2.4 Link Styles

| State | Color | Decoration |
|---|---|---|
| Default | `#2b3b84` | `none` (theme), `underline` + `font-weight: bolder` (content area) |
| Hover | `#18214a` | `underline` |

---

## 3. Buttons

### 3.1 Base Button Style

```
font-weight: 600
padding: 15px 30px
border-radius: 0  (sharp corners — no rounding)
line-height: 20px
```

Mobile override: `font-size: 12px; padding: 5px 8px; line-height: 15px`

### 3.2 Button Variants

| Variant | Background | Text | Border | Hover BG | Hover Text |
|---|---|---|---|---|---|
| **Blue (Primary)** | `#2b3b84` | `#fff` | `0.5px solid #fff` | `#fff` | `#2b3b84` |
| **Red (Secondary)** | `#e41e2f` | `#fff` | `0.5px solid #fff` | `#fff` | `#e41e2f` |
| **btn-primary** | `#2b3b84` | `#fff` | `#2b3b84` | `#222e67` | `#fff` |
| **btn-secondary** | `#e41e2f` | `#fff` | `#e41e2f` | `#c41826` | `#fff` |
| **Form submit** | `#2b3b84` | `#fff` | `0.5px solid gray` | `#fff` | `#2b3b84` |
| **Hero CTA** | `#2b3b84` | `#fff` | `solid (white)` | `#fff` | `#2b3b84` |

### 3.3 Button Behavior

- **Hover pattern**: Inverted fill — background and text colors swap on hover (characteristic of the brand)
- **Focus ring (btn-primary)**: `box-shadow: 0 0 0 0.2rem rgba(75, 88, 150, 0.5)`
- **Focus ring (btn-secondary)**: `box-shadow: 0 0 0 0.2rem rgba(232, 64, 78, 0.5)`
- **Hero CTA**: `max-width: 150px`, `font-size: 18px`, `padding: 9px 0`, centered text
- **Border radius**: Always `0` — square edges are a brand hallmark

---

## 4. Layout & Grid

### 4.1 Framework

Bootstrap 4.5.2 responsive grid with standard breakpoints:

| Breakpoint | Width | Token |
|---|---|---|
| Extra small | `0` | `--breakpoint-xs` |
| Small | `576px` | `--breakpoint-sm` |
| Medium | `768px` | `--breakpoint-md` |
| Large | `992px` | `--breakpoint-lg` |
| Extra large | `1200px` | `--breakpoint-xl` |

Mobile initialization threshold: `1240px`.

### 4.2 Page Structure

```
┌────────────────────────────────────────────────┐
│  Header (sticky, classic style)                │
│  ┌──────────┬──────────────────────────────┐   │
│  │  Logo    │  Main Navigation (centered)  │   │
│  └──────────┴──────────────────────────────┘   │
├────────────────────────────────────────────────┤
│  Hero Slider (full-width, layered overlays)    │
├────────────────────────────────────────────────┤
│  Content Sections (alternating full-bleed)     │
│  ┌─────────────────┬──────────────────────┐    │
│  │ Module (50%)     │ Module (50%)         │    │
│  └─────────────────┴──────────────────────┘    │
│  ┌──────┬──────┬──────┐                        │
│  │ Card │ Card │ Card │  (product grid)        │
│  └──────┴──────┴──────┘                        │
│  ┌────────┬────────┬────────┐                  │
│  │ 1/3    │ 1/3    │ 1/3    │ (info panels)   │
│  └────────┴────────┴────────┘                  │
├────────────────────────────────────────────────┤
│  Footer                                        │
│  ┌─────────┬─────────┬─────────┬──────────┐   │
│  │ Links   │ Links   │ Address │ Social   │   │
│  └─────────┴─────────┴─────────┴──────────┘   │
│  [Company logos row]                           │
│  © Copyright                                   │
└────────────────────────────────────────────────┘
```

### 4.3 Content Width

- Container: standard Bootstrap `.container` (responsive max-widths per breakpoint)
- Full-width sections: used for hero slider, background-image sections, and "Proudly Manufactured" banner
- Hero slider grid: `grid-template-columns: minmax(50px,1fr) minmax(0, 228px) minmax(0, 912px) minmax(50px,1fr)`

---

## 5. Header & Navigation

### 5.1 Header

- **Style**: Classic sticky header
- **Logo**: Left-aligned, `height: 122px` (desktop), `40px` (mobile)
- **Logo link area**: `height: 155px`, `padding: 15px 0 0`
- **Background**: White

### 5.2 Main Navigation

- **Layout**: Flexbox, horizontally centered
- **Menu item font**: Roboto, `1.2rem`, weight 300
- **Menu item padding-top**: `75px` (aligns text to bottom of header)
- **Hamburger icon color**: `#2b3b84`

### 5.3 Sub-menus (Dropdown)

- **Background**: `#e41e2f` (secondary red)
- **Min width**: `350px`
- **Text color**: `#fff`
- **Hover**: White background, gray text (`#868686`)
- **Padding**: `15px` top and bottom
- **Margin-left**: `20px`

---

## 6. Hero / Slider Section

### 6.1 Structure

- Full-width image slider (LayerSlider plugin)
- Canvas: `1280px × 720px` (scaled to full-width)
- Slide duration: `5000ms` with `transition2d: 5`

### 6.2 Overlay System

1. **Full dark overlay**: `rgba(0, 0, 0, 0.25)` covering entire slide
2. **Left blue bar**: `rgba(43, 59, 132, 0.78)` with `mix-blend-mode: multiply`, spanning left portion of grid
3. **Content sits on top** of both overlays at z-index 2

### 6.3 Content Pattern

```
H1 (bold, white, max-width 800px, text-shadow)
H6 (subtitle/tagline, white)
[CTA Button — "Read More"]
```

### 6.4 Text Shadow

- `text-shadow: 2px 2px 0px #080000` (typical value across slides)

### 6.5 Internal Page Heroes

- **Min-height**: `350px`
- **Blue overlay**: `66%` width (desktop), `80%` (mobile)
- **Same overlay color**: `rgba(43, 59, 132, 0.78)` with multiply blend
- Full dark underlay: `rgba(0, 0, 0, 0.25)`

---

## 7. Cards & Interactive Modules

### 7.1 Product Hover Cards

- **Background**: `#ffffff`
- **Border**: `1px solid #e5e5e5`
- **Padding**: `40px 20px`
- **Layout**: 3-column grid (one-third each)
- **Content**: H6 heading + description text + right-arrow icon (`icon-right-open-mini`)
- **Hover effect**: Color change / highlight (via `.hover_color` component)

### 7.2 Feature Modules (Quality Commitment / Customer Service)

- **Layout**: Two 50% columns side-by-side
- **Style**: Full background-image cover, white text overlay
- **Content**: H3 heading + H6 description + outlined button
- **Animated text**: Revealed on hover/interaction
- **Move-up effect**: `margin-top: -100px` to overlap with hero section above

### 7.3 "Quality Steel Difference" Section

- Three equal columns (one-third each):
  1. **Secondary background** (`#e41e2f`): Title "The Quality Steel Difference"
  2. **Primary background** (`#2b3b84`): Body copy + bullet list
  3. **Transparent/image**: Product imagery (tanks)
- White text throughout

---

## 8. Footer

### 8.1 Structure

- **Widgets section**: 4-column layout (one-fourth each)
  - Column 1: "Contacting Quality Steel" — navigation links
  - Column 2: "Quick Links" — navigation links
  - Column 3: "Headquarters" — address + phone
  - Column 4: Social media (LinkedIn icon)
- **Company logos row**: Horizontally aligned logos for all LT Corporation subsidiaries
- **Copyright bar**: Centered, muted text

### 8.2 Footer Styling

| Element | Style |
|---|---|
| **Background** | Dark (inherited from BeTheme) |
| **Widget padding** | `50px 0 30px` |
| **Link color** | `rgba(255, 255, 255, 0.25)` |
| **Link hover** | `#fff` |
| **Link font-size** | `18px` |
| **Link transition** | `0.3s` ease |
| **Heading (h4)** | Visible, white, `margin-bottom: 10px` |
| **Copyright text** | `rgba(255, 255, 255, 0.25)` |
| **Copyright margin** | `margin-bottom: 50px` (desktop), `20px` (mobile) |
| **Social icon** | Color `#2b3b84`, bg `rgba(255,255,255,0.25)`, `padding: 8px`, `font-size: 1.2rem` |
| **Margin top** | `25px` |

### 8.3 Company Logo Bar

- Horizontal row with `row-gap: 30px`
- Each logo: `max-width: 80px`, `w-100` (responsive)
- LT Corp logo (first): `width: 100px` with right border separator
- Logos link to respective company websites

---

## 9. Imagery & Photography

### 9.1 Style

- **Industrial photography**: Manufacturing floors, welding in action, steel tanks
- **People-focused**: Workers in safety gear, team photos
- **Product shots**: Aboveground/underground tanks on white or clean backgrounds
- **Patriotic elements**: U.S. flag imagery in "Proudly Manufactured" section

### 9.2 Background Patterns

- **Line pattern**: Repeating line texture (`line-pattern.jpg`) used as subtle background in content sections
- **White line pattern**: Light variant (`line-pattern-white.jpg`) used in product grid areas

### 9.3 Image Treatment

- `scale-with-grid` class for responsive images
- `bg-cover` for background images (`background-size: cover; background-position: center`)
- Lazy loading via `loading="lazy"` and `srcset` for responsive image serving

---

## 10. Spacing & Rhythm

### 10.1 General Spacing

| Context | Value |
|---|---|
| **Section margin-bottom** | `70px` (hero backgrounds) |
| **Column margin** | Varies: `0px`, `10px`, `20px`, `30px`, `40px`, `50px` |
| **Card padding** | `40px 20px` |
| **Hero content margin-bottom** | `80px` (desktop), `0` (mobile) |
| **Hero H6 margin-bottom** | `30px` |
| **Hero H1 margin-bottom** | `15px` |
| **Footer widgets padding** | `50px 0 30px` |
| **Line separator (hr.no_line)** | `margin: 0 auto 20px auto` |

### 10.2 Bootstrap Spacing Scale (custom)

| Token | Value |
|---|---|
| `--spacing-20` | `0.44rem` |
| `--spacing-30` | `0.67rem` |
| `--spacing-40` | `1rem` |
| `--spacing-50` | `1.5rem` |
| `--spacing-60` | `2.25rem` |
| `--spacing-70` | `3.38rem` |
| `--spacing-80` | `5.06rem` |

---

## 11. Animation & Transitions

| Element | Effect |
|---|---|
| **Slider transitions** | `transition2d: 5`, 5-second duration per slide |
| **Footer links** | `transition: color 0.5s ease` / `transition: 0.3s` |
| **Animated text** | `.animated-text` class — content reveals on interaction (hover/scroll) |
| **Button hover** | Inverted fill (smooth transition from filled to outlined appearance) |
| **Sub-menu hover** | Background and text color change |
| **Move-up modules** | Negative top margin (`margin-top: -100px`) to overlap previous section |

---

## 12. Accessibility

- **Accessibility widget**: ACSB app (accessiBe) integrated for WCAG compliance
  - Lead/trigger color: `#146FF8`
  - Position: Bottom-right
  - Trigger: People icon, medium size
- **ARIA labels**: Applied to menus, search, responsive toggles
- **`aria-expanded`**: Used on side slide menu
- **Screen reader text**: Standard WordPress `.screen-reader-text` class with clip-path

---

## 13. Iconography

- **Icon font**: Font Awesome (via BeTheme bundle)
- **Key icons used**:
  - `icon-right-open-mini` — card navigation arrows
  - `icon-menu-fine` — mobile hamburger menu
  - `icon-cancel-fine` — close/dismiss
  - `icon-linkedin` — social media
  - `icon_search` — custom SVG search icon (26px, stroke-based)

---

## 14. Forms

- **Form framework**: Gravity Forms (WordPress plugin)
- **Input styling**: `height: 37px`, `padding: 5px`, `margin-bottom: 20px`
- **Submit button**: `background-color: #2b3b84`, white text, `0.5px solid gray` border
- **Submit hover**: White background, `#2b3b84` text
- **reCAPTCHA**: Integrated (C4WP plugin)
- **Form fields**: Two-column layout with `15px` horizontal padding

---

## 15. Brand Voice & Messaging Patterns

### 15.1 Taglines (recurring)

- "Safety First, Quality Obsessed, Customer Driven"
- "Made in the U.S.A. since 1957"
- "Proudly Manufactured in the U.S.A."
- "Quality Steel Difference"
- "Reliable on-time delivery, service beyond the sale and communication at the highest level"

### 15.2 Content Tone

- **Professional and confident** — industry authority with 60+ years of heritage
- **Family-oriented** — "family and employee-owned company"
- **Customer-centric** — emphasis on service, partnership, and reliability
- **Quality-focused** — certifications (ASME, UL), 5-year warranties
- **American pride** — "U.S.A." and patriotic imagery permeate the brand

### 15.3 Page Pattern

Every interior page follows this structure:
1. **Hero banner** with blue overlay and H1 title
2. **H3 sub-heading** introducing the topic
3. **Body copy** with supporting details
4. **"Proudly Manufactured in the U.S.A."** closing footer banner

---

## 16. Responsive Design Summary

| Feature | Desktop | Tablet | Mobile |
|---|---|---|---|
| **Layout** | Full-width, multi-column | Adapts (one-half, one-third) | Single column |
| **Header height** | `155px` (logo area) | Reduced | `65px` |
| **Logo** | `122px` height | Scaled | `40px` max |
| **Hero** | `760px` min-height | — | Auto height, `65px` top padding |
| **Nav** | Horizontal, centered | Horizontal | Side-slide hamburger menu |
| **Buttons** | `15px 30px` padding | — | `5px 8px` padding, `12px` font |
| **Columns** | 2–3 column grids | 2 column | Single column stack |
| **Footer** | 4-column widgets | — | Stacked, absolute positioning adjustments |

---

## 17. Technology Stack (Website)

| Layer | Technology |
|---|---|
| **CMS** | WordPress 6.9.1 |
| **Parent theme** | BeTheme v27.6.1 |
| **Child theme** | Quality-Steel v0.0.1 |
| **CSS framework** | Bootstrap 4.5.2 (customized via Sass) |
| **Font provider** | Google Fonts (Roboto) |
| **Slider** | LayerSlider 8.1.2 + Slider Revolution 6.2.18 |
| **Forms** | Gravity Forms, WPForms |
| **CAPTCHA** | C4WP (reCAPTCHA) |
| **SEO** | Yoast SEO v27.0 |
| **Analytics** | Google Analytics (UA-143450451-1), Google Tag Manager (GTM-WQ5Z794) |
| **Accessibility** | accessiBe (ACSB) |
| **jQuery** | v3.7.1 (via WordPress core) |

---

## 18. Design Tokens Summary (for Application Development)

When building applications under the Quality Steel / LT Corporation brand, use these tokens as the design foundation:

```css
:root {
	/* Brand Colors */
	--qs-primary: #2b3b84;
	--qs-primary-hover: #222e67;
	--qs-primary-active: #1e2a5e;
	--qs-secondary: #e41e2f;
	--qs-secondary-hover: #c41826;
	--qs-tertiary: #606ca3;
	--qs-dark-red: #aa121f;

	/* Neutrals */
	--qs-body-text: #212529;
	--qs-gray: #868686;
	--qs-light-gray: #dfe2ed;
	--qs-light: #f8f9fa;
	--qs-dark: #343a40;
	--qs-white: #ffffff;
	--qs-black: #000000;

	/* Functional */
	--qs-success: #28a745;
	--qs-warning: #ffc107;
	--qs-danger: #dc3545;
	--qs-info: #17a2b8;

	/* Typography */
	--qs-font-family: "Roboto", Helvetica, Arial, sans-serif;
	--qs-font-mono: SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;

	/* Overlays */
	--qs-overlay-dark: rgba(0, 0, 0, 0.25);
	--qs-overlay-blue: rgba(43, 59, 132, 0.78);
	--qs-footer-muted: rgba(255, 255, 255, 0.25);

	/* Borders */
	--qs-border-light: #e5e5e5;
	--qs-btn-radius: 0;
}
```

---

## 19. Key Design Principles (Extracted from Brand)

1. **Sharp, authoritative geometry** — Zero border-radius on buttons and cards conveys industrial precision
2. **High-contrast color blocking** — Bold primary blue and secondary red sections with white text
3. **Layered depth** — Overlays with blend modes create rich hero compositions
4. **Patriotic palette** — Red, white, and blue align with "Made in the U.S.A." identity
5. **Typography restraint** — Single font family (Roboto) at limited weights maintains clean professionalism
6. **Inverted hover states** — Buttons swap fill/text colors on hover, creating a tactile interaction feel
7. **Content hierarchy** — Consistent H1 > H6 subtitle > CTA pattern across all hero sections
8. **Section rhythm** — Alternating full-bleed colored/image backgrounds create visual cadence down the page
