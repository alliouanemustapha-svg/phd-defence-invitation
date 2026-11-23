# PhD Defence Invitation

An interactive digital invitation to the PhD thesis defence of **Soumia Gouahi**, plus the small tool used to remove the dress-code block from the invitation video.

> *Groundwater and socio-economic Drought Assessment under global changes in the Souss-Massa Basin*
> Saturday 28 November 2026, 10:00, Faculty of Science, Ibn Zohr University, Agadir

## What's inside

```
phd-defence-invitation/
├── index.html                      # The interactive invitation (single self-contained page)
├── tools/
│   └── dress-code-eraser/
│       ├── erase.cs                # Frame-by-frame eraser for the invitation video
│       └── build.ps1               # Builds erase.exe with the Windows C# compiler
├── .gitignore
└── README.md
```

### The invitation page (`index.html`)

A single HTML file with no build step and no dependencies to install.

- **Opening animation:** a rolled diploma tied with a navy ribbon and a gold "SG" wax seal. Tapping the seal unties the ribbon, unrolls the diploma and tosses graduation caps with gold and navy confetti (drawn on a `<canvas>`).
- **Invitation card:** the Arabic verse, the candidate's name, the thesis title, and toile-style floral corners drawn in SVG.
- **Live countdown** to 28 November 2026, 10:00 (Agadir time, UTC+1).
- **When & where**, with buttons that open Google Maps and add the event to Google Calendar.
- **RSVP:** the guest chooses yes or no, types a name and an optional message, and the page opens WhatsApp with the reply already written.
- Respects `prefers-reduced-motion` and works at phone width.

Fonts (Pinyon Script, Cormorant Garamond, Amiri) load from Google Fonts. Everything else is inline.

### The video tool (`tools/dress-code-eraser`)

A small C# program that removes the "Dress Code / Allowed Colors" block from the invitation video. The block is printed on the card, and the card moves while it slides out of the envelope, so the tool works frame by frame:

1. FFmpeg decodes the video to raw RGB frames.
2. In each frame the tool finds the white card (its top edge and width) and works out where the dress-code block is, scaled to the card's current size.
3. It fills that area by blending the paper above and below it. It skips pixels that belong to the navy envelope, so the envelope edge stays intact.
4. FFmpeg re-encodes the frames (H.264, CRF 16) and copies the original audio unchanged.

The block's position is set in `erase.cs` (`RX0`, `RX1`, `RY0`, `RY1`, `CW`), measured on a 1080×1920 video. Another design needs new values there.

## Requirements

| Part | Needs |
| --- | --- |
| Invitation page | Any modern browser |
| Video tool | Windows with .NET Framework 4.x (includes `csc.exe`) and [FFmpeg](https://ffmpeg.org/) on the `PATH` |

## Running it

### Open the invitation

Open `index.html` in a browser, or serve the folder:

```powershell
python -m http.server 8000
```

Then go to `http://localhost:8000`.

To publish it, any static host works (GitHub Pages, Netlify, Vercel). For GitHub Pages: open the repository's **Settings → Pages**, choose the `main` branch and the root folder, and save.

### Build and run the video tool

```powershell
.\tools\dress-code-eraser\build.ps1
```

```powershell
.\tools\dress-code-eraser\erase.exe "invitation.mp4" "invitation_sans_dress_code.mp4" "log.txt"
```

If the build stops with `CS1619: Cannot create temporary file`, the folder path is too long for the compiler: clone the repository into a shorter path (for example `C:\dev`) and build again.

The third argument is a log file that lists the card position found in each frame. Add a fourth argument (any value, for example `debug`) to run only the detection and write the log, without encoding a video.

## Configuration

There are no API keys, passwords or environment variables, so the project has no `.env` file and needs no `.env.example`.

All settings live at the top of the script in `index.html`:

| Setting | Meaning |
| --- | --- |
| `WHATSAPP` | Phone number that receives RSVPs, international format without `+` (for example `2126XXXXXXXX`). Leave it empty and WhatsApp opens so the guest can pick the contact. |
| `EVENT` | Date and time of the defence, used by the countdown. |

The calendar link (in the "Add to calendar" button) stores the time in UTC: `20261128T090000Z` is 10:00 in Agadir.

## Not included

The original and edited videos are not in the repository. `.gitignore` excludes media files to keep personal videos out and the repository small.
