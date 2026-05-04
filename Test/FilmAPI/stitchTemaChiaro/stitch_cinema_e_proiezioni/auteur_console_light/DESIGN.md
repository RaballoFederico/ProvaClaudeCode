# Design System Specification: High-End Editorial Light Mode

## 1. Overview & Creative North Star: "The Pristine Gallery"
This design system is a light-mode evolution of a cinematic powerhouse. The Creative North Star is **"The Pristine Gallery."** Imagine a high-end art exhibition: white walls, intentional spotlighting, and a sense of infinite space. We are moving away from the "black box" theater and into the "architectural studio" aesthetic. 

The system breaks the "template" look by prioritizing **intentional asymmetry** and **tonal depth**. We do not use borders to define space; we use light and physics. By leveraging the contrast between the sharp `Epilogue` display faces and the precision of the `Inter` body types, we create a layout that feels like a premium digital magazine—airy, authoritative, and expensive.

---

## 2. Colors: Tonal Architecture
The palette is built on a foundation of "Cinematic Red" (`#E50914`) set against a sophisticated range of off-whites and cool grays.

### Primary & Brand Accents
- **Primary (`#b8000b` / `#e50914`):** Use sparingly. This is your "Lead Actor." It should command attention in CTAs and critical status indicators.
- **The Signature Texture:** For Hero sections or high-impact buttons, use a subtle linear gradient (135°) from `primary` to `primary_container`. This prevents the red from feeling "flat" and adds a backlit, cinematic glow.

### The "No-Line" Rule
**Explicit Instruction:** Designers are prohibited from using 1px solid borders for sectioning or containment. Boundaries must be defined solely through background color shifts. 
- Use `surface` (`#f9f9f9`) for the main canvas.
- Use `surface_container_low` (`#f3f3f3`) to carve out secondary content areas.
- This creates a seamless, "molded" look rather than a "boxed" look.

### Glass & Gradient Rule
To achieve a signature feel, floating elements (modals, dropdowns, navigation bars) should utilize **Glassmorphism**.
- **Surface:** `surface_container_lowest` (`#ffffff`) at 80% opacity.
- **Effect:** `backdrop-blur: 20px`.
- This ensures the UI feels like layered sheets of fine paper or frosted glass, allowing the "Cinematic Red" to bleed through softly when positioned underneath.

---

## 3. Typography: Editorial Authority
Typography is the backbone of the editorial feel. We pair the geometric character of `Epilogue` with the functional clarity of `Inter`.

| Role | Font Family | Size | Intent |
| :--- | :--- | :--- | :--- |
| **Display LG** | Epilogue | 3.5rem | The "Masthead." Use for hero titles with tight letter-spacing. |
| **Headline MD** | Epilogue | 1.75rem | Section intros. Always use high-contrast `on_surface` color. |
| **Title LG** | Inter | 1.375rem | Bold, utilitarian headers for cards and modules. |
| **Body MD** | Inter | 0.875rem | Default reading text. Use `on_surface_variant` for secondary info. |
| **Label MD** | Inter | 0.75rem | All-caps, tracked out (+5%) for metadata or tags. |

**Editorial Tip:** Use "Negative Leading." For large Display styles, set the line height to 1.1x to create a dense, modern typographic block that feels custom-tailored.

---

## 4. Elevation & Depth: Tonal Layering
In this system, depth is a result of light, not lines.

- **The Layering Principle:** 
  - Level 0 (Base): `surface` (`#f9f9f9`)
  - Level 1 (Sections): `surface_container_low` (`#f3f3f3`)
  - Level 2 (Cards): `surface_container_lowest` (`#ffffff`)
- **Ambient Shadows:** Shadows are reserved only for elements that "float" above the layout (e.g., Modals). Use a shadow with a 40px blur, 0px spread, and 4% opacity using `on_surface` as the tint. It should be felt, not seen.
- **The "Ghost Border" Fallback:** If accessibility requires a border, use `outline_variant` (`#e9bcb6`) at 20% opacity. This creates a "watermark" edge that defines the shape without interrupting the visual flow.

---

## 5. Components: Editorial Refinement

### Buttons
- **Primary:** `primary_container` background with `on_primary_container` text. Apply `DEFAULT` (0.25rem) roundness. 
- **Secondary:** `surface_container_highest` background. No border.
- **Tertiary:** Text-only in `primary` color, using a slight 0.05rem bottom padding and a subtle `primary` underline on hover.

### Cards & Lists
- **The Rule:** No dividers. 
- **Execution:** Use vertical white space (32px or 48px) to separate list items. For cards, use a `surface_container_lowest` background on a `surface_container` section.
- **Interaction:** On hover, a card should not move up; instead, the background should shift from `surface_container_lowest` to `surface_bright` or increase shadow density by 2%.

### Input Fields
- **Style:** Minimalist. `surface_container_high` background with a `none` border. 
- **Active State:** The bottom 2px of the input should transform into a `primary` red line. This mimics the "underlining" of an editor's pen.

### Chips
- **Action Chips:** Use `secondary_fixed` (`#ffdad5`) with `on_secondary_fixed` text. The roundness should be `full` to provide a soft contrast to the sharp editorial grid.

---

## 6. Do's and Don'ts

### Do
- **Do** use whitespace as a functional element. If it feels like "too much" space, add 16px more.
- **Do** use `primary` red for highlights, such as a single word in a headline or a small notification dot.
- **Do** lean into asymmetry. Align a headline to the left and the body text to a 66% width column to create an editorial layout.

### Don't
- **Don't** use pure black (`#000000`). Use `on_surface` (`#1a1c1c`) to maintain a premium, ink-on-paper feel.
- **Don't** use standard Material Design drop shadows. They feel "app-like" and destroy the editorial "magazine" vibe.
- **Don't** use 1px dividers to separate content. Use a 40px gap or a background color shift instead.

### Accessibility Note
While maintaining the "High-End" feel, ensure that `on_surface_variant` on `surface` backgrounds maintains a 4.5:1 contrast ratio. When using `primary` red on light backgrounds, always verify the text is legible or use the `on_primary_container` token for maximum clarity.