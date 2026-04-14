# Design System Document: The Cinematic Director’s Suite

This design system is engineered to transform a standard cinema management interface into a high-end editorial experience. We are moving away from the "data-heavy spreadsheet" aesthetic and toward a "backstage pass" feel—sophisticated, dark, and focused.

## 1. Overview & Creative North Star: "The Digital Auteur"
The Creative North Star for this system is **The Digital Auteur**. Cinema is about lighting, layering, and focus. Consequently, this system rejects the flat, boxed-in nature of traditional Bootstrap layouts. 

Instead of rigid grids and 1px borders, we use **Tonal Depth** and **Intentional Asymmetry**. We treat the UI as a series of lit stages. Data isn't just stored; it is presented. By using high-contrast typography scales and overlapping surface layers, we create a sense of prestige that mirrors the silver screen itself.

## 2. Colors: Depth Over Definition
The palette is a "Noir Modern" theme. We use a deep charcoal base with "Golden Hour" (Secondary) and "Premiere Red" (Primary) accents.

### The "No-Line" Rule
**Explicit Instruction:** Designers are prohibited from using 1px solid borders to section off content. Boundaries must be defined solely through background color shifts. 
*   *Implementation:* Use `surface-container-low` (#1C1B1B) for the main background and `surface-container` (#201F1F) for sidebar elements.

### Surface Hierarchy & Nesting
Treat the UI as physical layers. Each "inner" container should move up the hierarchy:
1.  **Base Layer:** `surface` (#131313)
2.  **Sectioning:** `surface-container-low` (#1C1B1B)
3.  **Interactive Cards:** `surface-container-high` (#2A2A2A)
4.  **Floating Modals:** `surface-container-highest` (#353534)

### The "Glass & Gradient" Rule
To avoid a "flat" digital feel, use Glassmorphism for floating navigation or hovering "Now Playing" cards. Use `surface_variant` (#353534) at 60% opacity with a `20px` backdrop-blur. 
*   **Signature Texture:** Apply a subtle linear gradient to Primary buttons: `primary-container` (#E50914) to `inverse-primary` (#C0000C) at a 135-degree angle.

## 3. Typography: Editorial Authority
We pair two sans-serifs to create a hierarchy that feels like a film credit sequence.

*   **Display & Headlines (Manrope):** This is our "Editorial" voice. Use `display-lg` (3.5rem) for theater occupancy percentages or daily revenue. Use `headline-sm` (1.5rem) for movie titles in list views.
*   **Body & Labels (Inter):** Our "Functional" voice. Highly legible at small scales. Use `label-md` (0.75rem) for metadata like "Runtime" or "Rating."
*   **Contrast as Hierarchy:** Never use "Bold" when you can use "Scale." A large `display-md` in a light weight is more premium than a small bold header.

## 4. Elevation & Depth: Tonal Layering
Traditional shadows are too "web 2.0." We use **Ambient Lighting**.

*   **The Layering Principle:** To lift a movie poster card from the background, do not add a shadow. Instead, place the `surface-container-high` card on a `surface-container-low` background. The delta in hex value creates the lift.
*   **Ambient Shadows:** For floating dropdowns, use a shadow with a 40px blur, 0% spread, and 8% opacity using the `on-surface` color. It should feel like a soft glow, not a drop shadow.
*   **The "Ghost Border" Fallback:** If a table row needs separation, use `outline-variant` (#5E3F3B) at **15% opacity**. It should be felt, not seen.

## 5. Components

### Buttons
*   **Primary (Action):** `primary-container` background with `on-primary-container` text. Use `xl` (0.75rem) roundedness. No border.
*   **Tertiary (Management):** Ghost style. No background, `secondary` (#E9C176) text. On hover, shift background to `secondary_container` at 20% opacity.

### Tables & Lists (Relational Data)
*   **Forbid Dividers:** Do not use `<hr>` or `border-bottom`. Use the Spacing Scale `spacing-4` (0.9rem) to create clear air between rows.
*   **Alternating Tones:** Use `surface-container-lowest` for the table header and `surface-container-low` for every second row.
*   **Status Chips:** Use `tertiary_container` for "Scheduled" and `error_container` for "Sold Out." Keep text in `on-tertiary-container` for high-end legibility.

### Input Fields
*   **Style:** Minimalist. Use `surface-container-highest` as the fill. 
*   **Focus State:** Do not use a blue glow. Use a 1px "Ghost Border" of `primary` (#FFB4AA) and a slight internal tint shift.

### Cinema-Specific Components
*   **The Seat Map:** Use `secondary_fixed_dim` for available seats and `primary_container` for selected seats. Use `md` (0.375rem) roundedness to mimic luxury lounger shapes.
*   **Timeline Scrubber:** For movie scheduling, use a `surface-variant` track with a `primary` thumb.

## 6. Do’s and Don'ts

### Do
*   **DO** use whitespace as a separator. If you think you need a line, try adding `spacing-8` (1.75rem) instead.
*   **DO** use `secondary` (Gold) for "VIP" or "Premium" features to denote value.
*   **DO** ensure all interactive elements have a minimum touch/click target of 44px, despite the compact "professional" look.

### Don’t
*   **DON'T** use pure black (#000000). Use `surface` (#131313) to allow for depth and "on-surface" contrast.
*   **DON'T** use Bootstrap's default blue. Every color must map to the provided tokens to maintain the "Cinematic" mood.
*   **DON'T** crowd the sidebar. Use `spacing-12` (2.75rem) padding to give the navigation "breathing room," making the system feel less like a tool and more like a dashboard.