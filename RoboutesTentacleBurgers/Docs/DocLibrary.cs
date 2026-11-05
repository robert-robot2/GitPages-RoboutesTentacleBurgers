namespace GhostDocs;

/// <summary>
/// The GhostX documentation library.  One DocSection per README topic.
/// Add a section = add one entry to BuildSections().  Placeholder sections are flagged so you can find them.
/// </summary>
public static class DocLibrary
{
    public static readonly Category[] Categories =
    [
        new("start",     "Getting Started",   "🚀"),
        new("editor",    "Editor",            "🧭"),
        new("gameplay",  "Gameplay",          "🎮"),
        new("ui",        "UI System",         "🖥️"),
        new("tutorials", "Tutorials",         "📘"),
        new("arch",      "Architecture",      "🧬"),
        new("scripting", "Scripting & Tools", "⚡"),
        new("blazor",    "Blazor Integration","🌐"),
        new("ref",       "Reference",         "📚"),
    ];

    public static readonly RoadmapGroup[] Roadmap = BuildRoadmap();
    public static readonly DocSection[] Sections = BuildSections();

    public static DocSection? Find(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : Sections.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static int RoadmapTotal => Roadmap.Sum(g => g.Items.Length);
    public static int RoadmapDone => Roadmap.Sum(g => g.Done);
    public static int RoadmapPercent => RoadmapTotal == 0 ? 0 : (int)Math.Round(100.0 * RoadmapDone / RoadmapTotal);

    // ─────────────────────────────────────────────────────────
    //  Block helpers
    // ─────────────────────────────────────────────────────────
    static class B
    {
        public static Block Hero() => new() { Kind = BlockKind.Hero };
        public static Block H2(string id, string t) => new() { Kind = BlockKind.H2, Id = id, Text = t };
        public static Block H3(string t) => new() { Kind = BlockKind.H3, Text = t };
        public static Block P(string t) => new() { Kind = BlockKind.P, Text = t };
        public static Block Note(string tone, string title, string t) => new() { Kind = BlockKind.Callout, Tone = tone, Id = title, Text = t };
        public static CodeTab Tab(string label, string lang, string code) => new(label, lang, code.TrimEnd());
        public static Block Code(params CodeTab[] tabs) => new() { Kind = BlockKind.Code, Tabs = tabs };
        public static Block Table(string[] headers, string[][] rows, bool keys = false, bool filter = false) =>
            new() { Kind = BlockKind.Table, Headers = headers, Rows = rows, Keys = keys, Filter = filter };
        public static Block Steps(params string[] items) => new() { Kind = BlockKind.Steps, Items = items };
        public static Block Cards(params Card[] c) => new() { Kind = BlockKind.Cards, Cards = c };
        public static Block List(params string[] items) => new() { Kind = BlockKind.List, Items = items };
        public static Block Check(params string[] items) => new() { Kind = BlockKind.Checklist, Items = items };
        public static Block Stats(params string[] items) => new() { Kind = BlockKind.Stats, Items = items };
        public static Block Diagram(string[] nodes, string[] arrows) => new() { Kind = BlockKind.Diagram, Items = nodes, Headers = arrows };
        public static Block Faq(params string[] items) => new() { Kind = BlockKind.Faq, Items = items };
        public static Block RoadmapView() => new() { Kind = BlockKind.Roadmap };
    }

    // ─────────────────────────────────────────────────────────
    //  SECTIONS
    // ─────────────────────────────────────────────────────────
    static DocSection[] BuildSections() =>
    [
        // ═════════════════════════  GETTING STARTED  ═════════════════════════
        new("introduction", "start", "Introduction", "👻",
            "GhostX is a custom renderer engine: a native C++ DirectX 12 RHI, a C# WinForms docking editor and a Blazor WASM UI.",
            ["overview", "welcome", "ghostx", "engine"],
        [
            B.Hero(),
            B.H2("what-is-ghostx", "What is GhostX?"),
            B.P("GhostX is a custom renderer engine built on a **native C++ DirectX 12 renderer (RHI-style)** with a **C# WinForms editor shell** and a **Blazor WASM UI**. The editor uses a Visual Studio-style docking suite, so panels can be docked, tabbed or floated."),
            B.P("It ships with everything needed to build and package a small game end to end: scene editing, PBR materials, shadows, anti-aliasing, sprite animation, gamepad support, an audio mixer, a UI designer and a one-click standalone packager."),
            B.Stats("__PHASES_DONE__|Phases shipped", "__PHASES_TOTAL__|Phases on the roadmap", "1|RHI backend live (DX12)", "2,048|Max scene objects"),
            B.H2("feature-tour", "Feature tour"),
            B.Cards(
                new Card("🎨", "DirectX 12 RHI", "Backend-agnostic scene model feeding an IGhostRHI interface. DX12 today, Vulkan and friends later.", "rhi-backends"),
                new Card("🧩", "Docking Editor", "Hierarchy, Details, Palette, Content Browser, UI Designer and Animation Editor in a VS-style dock.", "panels-windows"),
                new Card("🕹️", "Play Mode", "Press Ctrl+P to play in the editor or in a standalone window. Stopping restores the scene exactly.", "play-mode"),
                new Card("🌐", "Blazor WASM UI", "Interactive docs, store and mod library hosted through WebView2, wired live to the renderer.", "blazor-setup"),
                new Card("🎞️", "Sprite Animation", "Sprite sheet editor, .ghostanim files and per-clip textures for animation states.", "sprite-animation"),
                new Card("📦", "One-click Packaging", "Package Project builds three MSBuild runs and drops a portable folder with an exe, optionally zipped.", "package-game")),
            B.H2("hello-ghost", "Hello, Ghost"),
            B.P("The smallest possible editor-side snippet. It spins up the engine loop from a WinForms host and sets the clear color through the exported C API."),
            B.Code(
                B.Tab("C#", "csharp", """
// GhostForm.cs  (simplified)
public partial class GhostForm : Form
{
    private readonly System.Windows.Forms.Timer _loop = new() { Interval = 1 };

    public GhostForm()
    {
        InitializeComponent();
        Load += (_, _) =>
        {
            GhostEngine.GhostEngine_Init(Handle, ClientSize.Width, ClientSize.Height);
            GhostEngine.GhostEngine_SetClearColor(0.02f, 0.05f, 0.12f);   // deep ghost blue
            _loop.Tick += (_, _) => GhostEngine.GhostEngine_Frame();
            _loop.Start();
        };
    }
}
"""),
                B.Tab("Java", "java", """
// A tiny Java-side helper that pre-checks a project folder before you open it in GhostX.
import java.nio.file.*;

public class HelloGhost {
    public static void main(String[] args) throws Exception {
        Path project = Path.of(args.length > 0 ? args[0] : "TestGame");
        boolean ok = Files.exists(project.resolve("TestGame.ghostproject"));
        System.out.println(ok ? "👻 project found: " + project.toAbsolutePath()
                              : "no .ghostproject here - did you File > New Project?");
    }
}
""")),
            B.Note("easter", "Official minimum spec", "The README lists a 4 MHz DOS computer with onboard GPU and a floppy drive. We respect the commitment to the bit. See Requirements for the real list."),
        ]),

        new("requirements", "start", "Requirements", "🧰",
            "Everything you need installed to run or build GhostX, plus the NuGet packages the shell uses.",
            ["prerequisites", "sdk", "visual studio", "webview2", "nuget", "system"],
        [
            B.H2("system", "System requirements"),
            B.Table(["Component", "Requirement", "Notes"],
            [
                ["OS", "Windows 10 / 11 (x64)", "The editor is WinForms; DX12 needs a modern Windows build."],
                ["GPU", "DirectX 12 compatible", "Required by the renderer."],
                [".NET", ".NET 10.0 SDK", "Needed to build the editor and Blazor UI."],
                ["IDE", "Visual Studio 2022+", "Workloads: ASP.NET and web development, Desktop development with C++."],
                ["Browser runtime", "WebView2 Runtime", "Hosts the Blazor WASM UI inside the editor."],
            ], filter: true),
            B.H2("nuget", "NuGet packages (GhostShell)"),
            B.List("`DockPanelSuite` - docking UI", "`DockPanelSuite.ThemeVS2015` - dark Visual Studio theme", "`Microsoft.Web.WebView2` - Blazor host", "`Assimp` - FBX and model import"),
            B.Code(B.Tab("bash", "bash", """
dotnet add GhostShell package DockPanelSuite
dotnet add GhostShell package DockPanelSuite.ThemeVS2015
dotnet add GhostShell package Microsoft.Web.WebView2
dotnet add GhostShell package Assimp
""")),
            B.Note("easter", "Legacy hardware", "README minimum: 4 MHz DOS OS computer, onboard GPU, floppy disk drive. Ghosts do not need a modern machine. Probably."),
        ]),

        new("install-build", "start", "Install & Build", "🛠️",
            "Download, build in Visual Studio and run GhostShell in x64 Release or Debug.",
            ["install", "build", "visual studio", "x64", "anycpu", "release", "debug"],
        [
            B.P("A downloadable engine release is coming. Until then, open the entire engine for editing by downloading the source and building it in Visual Studio."),
            B.H2("build-steps", "Build from source"),
            B.Steps(
                "Clone the repo|Download the GhostX solution from GitHub and open it in Visual Studio 2022 or newer.",
                "Restore packages|Let NuGet restore DockPanelSuite, WebView2 and Assimp for the GhostShell project.",
                "Pick a configuration|Use x64 Release or x64 Debug. AnyCPU Release also works. AnyCPU Debug currently cannot find the DLL.",
                "Build the solution|GhostX (C++) builds GhostX.dll and GhostGame.dll. A post-build event copies GhostGame.dll to the x64 Release folder.",
                "Run GhostShell|The WinForms editor launches with the DX12 viewport and docking panels."),
            B.Note("warn", "Configuration matrix", "x64 Release / Debug: working. AnyCPU Release: working. AnyCPU Debug: cannot find DLL (tracked in the roadmap under DEBUG / ERRORS)."),
            B.Table(["Configuration", "Status"],
            [
                ["x64 / Release", "✅ Working"],
                ["x64 / Debug", "✅ Working"],
                ["AnyCPU / Release", "✅ Working"],
                ["AnyCPU / Debug", "❌ Cannot find DLL"],
            ]),
            B.H2("reset-layout", "If the docking layout looks wrong"),
            B.P("Delete the saved layout XML and relaunch. The layout is saved on close, so a stale file can restore a broken arrangement."),
            B.Code(B.Tab("PowerShell", "bash", """
Remove-Item "$env:LocalAppData\GhostX\*.xml"
""")),
            B.P("You can also use **Window > Reset Layout** or **Window > Show Viewport** from inside the editor."),
        ]),

        new("notes-known-issues", "start", "Notes & Known Issues", "🩹",
            "What works, what is flaky and the workaround for each. Filter the table to find yours.",
            ["bugs", "errors", "issues", "workaround", "troubleshooting"],
        [
            B.H2("known-issues", "Known issues"),
            B.Table(["Issue", "Status", "Workaround"],
            [
                ["AnyCPU Debug cannot find DLL", "Open", "Use x64 Debug/Release or AnyCPU Release."],
                ["Dock Suite renders incorrectly", "Workaround", "Delete the XML in %LocalAppData%\\GhostX, or use Window > Show Viewport."],
                ["FBX imports at wrong scale", "Workaround", "Set Largest Side fit to 2 for a default, normal-scaled import."],
                ["Moved files lose their asset link", "Known", "Re-upload assets. The engine is not aware of file path changes yet."],
                ["Imported textures missing in Content Browser", "Investigating", "Textures still load. To be verified in a packaged build."],
                ["Textures reset when moved outside the engine", "Known", "No drag and drop to folders in the Content Browser yet."],
                ["Projects sometimes do not load scenes", "Investigating", "Open the scene manually from the Content Browser."],
                ["Roles do not reset settings when duplicated", "Investigating", "Possibly a Link / Custom ID tag issue."],
                ["Sprite Sheet Editor frames look slightly off in editor but not in game", "Known", "Cosmetic. Needs adjustment (see roadmap)."],
                ["Some parameters only work with certain roles", "By design", "Advanced menu addition planned (Phase 30)."],
            ], filter: true),
            B.H2("tips", "Handy tips"),
            B.Note("tip", "FBX import", "Set **Largest Side fit = 2**. The README flags that 2 should probably be the automatic default instead of 0."),
            B.Note("tip", "Snap to grid", "Set a value and Translate in the dropdown for the snap size, then click the **XYZ arrows**, not the object itself."),
            B.Note("tip", "Perk", "Your project or engine will not crash if you move files. You do have to re-upload assets for now."),
        ]),

        // ═════════════════════════  EDITOR  ═════════════════════════
        new("viewport-controls", "editor", "Viewport Controls", "🎥",
            "Fly, dolly, orbit and jump the editor camera. Keys only work while a viewport has focus.",
            ["camera", "wasd", "mouse", "orbit", "dolly", "fly"],
        [
            B.P("Camera keys only work while a viewport has focus, so typing in a panel never moves the camera. Hovering the viewport gives it keyboard focus."),
            B.H2("viewport-table", "Viewport input"),
            B.Table(["Input", "Action"],
            [
                ["`Right mouse` + move", "Look around"],
                ["`W` `A` `S` `D`", "Move the active camera"],
                ["`Space` or `E` / `Q`", "Move up / down"],
                ["`Mouse wheel`", "Dolly camera in / out"],
                ["`Shift` + `Mouse wheel`", "Dolly camera 4x faster"],
                ["`Right mouse` + `Wheel`", "Change fly camera speed"],
                ["`Mouse wheel` (orbit camera)", "Change orbit distance"],
                ["`Z`", "Jump camera to the selected object"],
                ["`Double-click` in Hierarchy", "Jump camera to that object"],
                ["`Double-click` a camera in Hierarchy", "Make it the main view camera"],
                ["`G`", "Toggle grid visibility"],
            ], keys: true, filter: true),
            B.Note("note", "Multi-camera", "Use **Camera > Show Viewport** to open a live window for a camera. Up to 4 camera windows at once."),
        ]),

        new("selecting-editing", "editor", "Selecting & Editing", "🖱️",
            "Select, move, duplicate, array, undo and rename. The red collider box shows on the selection.",
            ["select", "duplicate", "array", "undo", "redo", "copy", "paste", "rename", "delete"],
        [
            B.H2("selection-table", "Selection and editing input"),
            B.Table(["Input", "Action"],
            [
                ["`Left click`", "Select an object, light, camera or sky icon"],
                ["`Left drag`", "Move the selection on a camera-facing plane (locked objects cannot be dragged)"],
                ["`Shift` + `Left click`", "Pick one face (click again for All Faces)"],
                ["`F`", "Toggle face-pick mode"],
                ["`Delete`", "Delete the selection (the last camera cannot be deleted)"],
                ["`Ctrl` + `D`", "Duplicate the selected object"],
                ["`Ctrl` + `F`", "Array duplicate: line, grid, staircase, square outline or circle"],
                ["`Ctrl` + `C` / `Ctrl` + `V`", "Copy / paste"],
                ["`Ctrl` + `Z`", "Undo"],
                ["`Ctrl` + `Y` or `Ctrl` + `Shift` + `Z`", "Redo"],
                ["`F2`", "Rename the selected object"],
                ["`Right click` in Hierarchy", "Object context menu"],
                ["`Right click` in viewport", "Viewport context menu"],
            ], keys: true, filter: true),
            B.Note("tip", "Array Duplicate", "Ctrl+F lets you set counts and XYZ spacing for line, grid, staircase, square outline and circle layouts. Great for platforms, coin rows and stairs."),
            B.Note("note", "Red collider box", "Appears automatically around the selected object. Objects marked **Locked in place (Gameplay)** stay selectable but cannot be dragged or gizmo-edited."),
        ]),

        new("gizmos-tools", "editor", "Gizmos & Tools", "📐",
            "Translate, rotate, scale and dimension tools with axis constraints, grid snapping and typed units.",
            ["gizmo", "translate", "rotate", "scale", "dimension", "snap", "grid", "units"],
        [
            B.Table(["Input", "Action"],
            [
                ["Tool dropdown (top left)", "Select, Translate, Rotate, Scale, Dimension"],
                ["Drag a gizmo handle", "Move, rotate or scale along an axis (centre handle = uniform scale)"],
                ["`Shift` while dragging", "Toggle snapping"],
                ["`Esc` while dragging", "Cancel the drag and restore original values"],
                ["Click a dimension label or edge", "Type an exact size such as 10mm or 2in"],
                ["View menu", "Space (World/Local), units, decimals, grid size, plane and style, overlays"],
            ], keys: true),
            B.H2("exact-sizes", "Typing exact sizes"),
            B.P("Click a dimension label or an edge in the Dimension tool and type a value with units, like `10mm`, `2in` or `1.5m`. Units are also shown in the Details panel."),
        ]),

        new("adding-texturing", "editor", "Adding & Texturing", "🖼️",
            "Drop palette items into the scene, drag textures onto faces and import images or models.",
            ["palette", "texture", "import", "fbx", "drag", "drop", "content browser"],
        [
            B.Table(["Input", "Action"],
            [
                ["Drag a palette item into the viewport, or double-click it", "Add it to the scene"],
                ["Drag a texture tile onto an object", "Apply it (hold `Shift` for one face)"],
                ["Double-click a texture tile", "Apply it to the selection"],
                ["Drop images or 3D models onto the Content Browser", "Import them"],
                ["Double-click `.ghostscene` / `.ghostui` / `.ghostanim`", "Open it"],
            ], keys: true),
            B.Note("warn", "FBX scale", "When importing FBX meshes set **Largest Side fit** to 2 for a default normal-scaled import."),
        ]),

        new("panels-windows", "editor", "Panels & Windows", "🪟",
            "Dockable panels, layout persistence and the Window menu.",
            ["dock", "panel", "layout", "window", "save", "reset"],
        [
            B.Table(["Input", "Action"],
            [
                ["`Ctrl` + `S`", "Save the scene (or the open UI / animation file if that window has focus)"],
                ["`4`", "Hide or show all tool panels"],
                ["Drag a panel tab", "Dock, tab or float it"],
                ["Window menu", "Show Viewport, Palette, Hierarchy, Details, Content Browser, UI Designer, Animation Editor, Reset Layout"],
                ["Camera > Show Viewport", "Open a live window for that camera (up to 4)"],
            ], keys: true),
            B.P("The layout is saved when you close the editor, into an XML file under `%LocalAppData%\\GhostX`."),
            B.Diagram(
                ["Hierarchy¦left dock¦objects¦cameras¦lights",
                 "Viewport¦center¦DX12 swap chain¦gizmos¦grid",
                 "Details¦right dock¦Transform¦Material¦Role"],
                ["", ""]),
        ]),

        // ═════════════════════════  GAMEPLAY  ═════════════════════════
        new("play-mode", "gameplay", "Play Mode", "▶️",
            "Test in the editor or in a standalone window. Stopping Play restores the scene exactly as it was.",
            ["play", "standalone", "pause", "stop", "game module"],
        [
            B.P("The editor camera and gizmos are off while playing. Stopping Play restores the scene exactly as it was."),
            B.Table(["Input", "Action"],
            [
                ["Play menu", "Start, pause and stop the game"],
                ["`W` `A` `S` `D` / Arrow keys", "Move (relative to the camera)"],
                ["`Shift`", "Run"],
                ["`Space`", "Jump (tap for a short hop, hold for full height)"],
                ["`F`", "Enter a portal set to Press F"],
                ["`Q` / `E`", "Turn a free camera"],
                ["`Right mouse` + move", "Orbit an orbit camera"],
                ["`Mouse wheel`", "Zoom an orbit camera"],
                ["`R`", "Restart the level"],
                ["`Ctrl` + `P`", "Enter Play mode"],
                ["`Shift` + `F5`", "Stop Play mode"],
            ], keys: true, filter: true),
        ]),

        new("mario-controls", "gameplay", "Mario Controls", "🍄",
            "Gamepad and keyboard bindings for Super Mario Infinitus. Some are still TBD.",
            ["gamepad", "keyboard", "xinput", "hid", "controls", "mario", "binds"],
        [
            B.H2("gamepad", "Gamepad"),
            B.Table(["Button", "Action"],
            [
                ["Left Digital (D-Pad)", "Move / menu navigation"],
                ["Right Digital (D-Pad)", "Camera orbit"],
                ["Left Analog Stick", "TBD"],
                ["Start", "Pause / menu"],
                ["Select", "TBD"],
                ["Button 1", "Jump"],
                ["Button 2", "Sprint / hold to grab shell"],
                ["Button 3", "Attack"],
                ["Button 4", "Use pipe"],
                ["L1", "TBD"],
                ["L2", "Zoom out"],
                ["R1", "Recenter camera"],
                ["R2", "Zoom in"],
                ["R3", "TBD"],
            ], filter: true),
            B.H2("keyboard", "Keyboard and mouse"),
            B.Table(["Key / Input", "Action"],
            [
                ["`WASD`", "Move / menu navigation"],
                ["`Right Mouse`", "Camera orbit"],
                ["`F10`", "Pause / menu"],
                ["`Space`", "Jump"],
                ["`Shift`", "Sprint / hold to grab shell"],
                ["`Left Mouse`", "Attack"],
                ["`F`", "Use pipe"],
                ["`Middle Mouse`", "Zoom"],
                ["`Z`", "Recenter camera"],
            ], keys: true, filter: true),
            B.Note("note", "TBD", "TBD marks a control that still needs to be assigned. Phase 37 covers gamepad zoom and recenter, Phase 39 covers key and gamepad binds."),
        ]),

        new("object-roles", "gameplay", "Object Roles", "🎭",
            "Roles give scene objects gameplay behavior. Placeholder: wire this table to the real role list.",
            ["role", "behavior", "enemy", "player", "coin", "goal", "portal", "boss"],
        [
            B.Note("placeholder", "Placeholder section", "Role names below come from the README's gameplay features. Replace with the real role registry and parameters."),
            B.Table(["Role", "Purpose", "Typical parameters"],
            [
                ["Player", "The controllable character, carries the orbit camera", "speed, jumpHeight, depth"],
                ["Solid", "Ground, steps, platforms", "collider"],
                ["QuestionBlock", "Hit from below to release coins or power-ups", "contents, breakable"],
                ["Coin", "Collectible counted by the HUD", "value"],
                ["Enemy", "Walks an enemy pathway, can drop a shell", "pathId, flying, shellDrop"],
                ["Goal", "Flag pole and castle. Loads the next scene", "nextScene, delay"],
                ["Portal", "Pipe or door. Use with F", "targetScene, pressF"],
                ["Boss", "End-of-level encounter", "health, pattern"],
            ], filter: true),
            B.Code(
                B.Tab("C#", "csharp", """
// Placeholder: a role behavior sketch. Hook into your real GhostGame API.
public sealed class CoinRole : IGhostRole
{
    public string Name => "Coin";
    public float Value { get; set; } = 1f;

    public void OnTouch(GhostObject self, GhostObject other)
    {
        if (other.Role != "Player") return;
        GhostGame.Hud.AddCoin((int)Value);
        GhostGame.Audio.Play("coin");
        self.Destroy();
    }
}
"""),
                B.Tab("JSON", "json", """
{
  "object": "Coin_03",
  "role": "Coin",
  "behavior": { "value": 1, "spin": true },
  "collider": true,
  "linkId": "coins.row1"
}
""")),
        ], Placeholder: true),

        new("sprite-animation", "gameplay", "Sprite Animation", "🎞️",
            "Sprite sheet editor, .ghostanim files, animation states and per-clip textures. Placeholder content.",
            ["sprite", "animation", "ghostanim", "states", "clips"],
        [
            B.Note("placeholder", "Placeholder section", "Fill in with screenshots and the exact .ghostanim schema."),
            B.H2("workflow", "Workflow"),
            B.Steps(
                "Create a sprite sheet|Open the Sprite Sheet Editor and slice a square-grid sheet into frames.",
                "Make clips|Open the Animation Editor and build clips from frame ranges. Each clip can have its own texture.",
                "Wire states|Map states like idle, run, jump and death to clips.",
                "Assign to a role|Select an object, open Details > Animation and pick the .ghostanim file."),
            B.Code(B.Tab("JSON", "json", """
{
  "format": "ghostanim",
  "version": 1,
  "name": "MarioSmall",
  "clips": [
    { "name": "idle", "texture": "Textures/mario_idle.png", "frames": [0], "fps": 1,  "loop": true },
    { "name": "run",  "texture": "Textures/mario_run.png",  "frames": [0,1,2], "fps": 12, "loop": true }
  ],
  "states": { "Idle": "idle", "Running": "run" }
}
""")),
        ], Placeholder: true),

        // ═════════════════════════  UI SYSTEM  ═════════════════════════
        new("ghostui-format", "ui", "GhostUI Format", "🧱",
            "The .ghostui JSON format: widgets, anchors, offsets and a complete working HUD.",
            ["ghostui", "hud", "json", "widgets", "canvas", "text", "button", "progressbar", "anchor"],
        [
            B.P("UI assets live in `Content/UI/*.ghostui`. They are JSON trees of widgets. Build them in the UI Designer or write them by hand."),
            B.H2("widget-types", "Widget types"),
            B.Table(["Type", "Use", "Key properties"],
            [
                ["Canvas", "Container and background panel", "anchor, offset, size, color, visible, children"],
                ["Text", "Labels and counters", "text, fontSize, align, color"],
                ["Button", "Clickable control", "text, fontSize, color"],
                ["ProgressBar", "Bars such as coin progress", "value (0-1), color, bgColor"],
            ], filter: true),
            B.Note("note", "Anchors seen in the README", "`Fill`, `TopLeft`, `TopRight`, `BottomCenter`, `Center`. For `align`, 1 is centered and 2 is right-aligned (0 is assumed to be left)."),
            B.H2("hud-json", "Complete HUD.ghostui"),
            B.Note("warn", "Fixed a typo", "The README version of this file was missing a comma between AgainButton and QuitButton. The version below is valid JSON."),
            B.Code(B.Tab("HUD.ghostui", "json", """
{
  "format": "ghostui",
  "version": 1,
  "name": "HUD",
  "widgets": [
    {
      "type": "Canvas", "name": "Root", "anchor": "Fill",
      "children": [
        {
          "type": "Canvas", "name": "StatsPanel", "anchor": "TopLeft",
          "offset": [20, 20], "size": [440, 190], "color": [0, 0, 0, 0.45],
          "children": [
            { "type": "Text", "name": "CoinText", "anchor": "TopLeft", "offset": [24, 10], "size": [400, 56],
              "text": "Coins 0 / 0", "fontSize": 48, "color": [1.0, 0.85, 0.2, 1] },
            { "type": "Text", "name": "EnemyText", "anchor": "TopLeft", "offset": [24, 68], "size": [400, 44],
              "text": "Enemies 0 / 0", "fontSize": 38, "color": [1, 1, 1, 1] },
            { "type": "Text", "name": "DeathText", "anchor": "TopLeft", "offset": [24, 114], "size": [400, 36],
              "text": "Deaths 0", "fontSize": 32, "color": [0.8, 0.8, 0.8, 1] },
            { "type": "ProgressBar", "name": "CoinBar", "anchor": "TopLeft", "offset": [24, 160], "size": [392, 16],
              "value": 0.0, "color": [1.0, 0.85, 0.2, 1], "bgColor": [0, 0, 0, 0.6] }
          ]
        },
        {
          "type": "Text", "name": "SizeText", "anchor": "TopRight", "offset": [-40, 30], "size": [300, 56],
          "text": "small", "fontSize": 48, "align": 2, "color": [0.8, 0.8, 0.8, 1]
        },
        {
          "type": "Text", "name": "HintText", "anchor": "BottomCenter", "offset": [0, -56], "size": [1500, 36],
          "text": "", "fontSize": 28, "align": 1, "color": [0.8, 0.8, 0.8, 1]
        },
        {
          "type": "Canvas", "name": "CompletePanel", "anchor": "Fill", "visible": false, "color": [0, 0, 0, 0.55],
          "children": [
            { "type": "Text", "name": "CompleteTitle", "anchor": "Center", "offset": [0, -140], "size": [1400, 110],
              "text": "LEVEL COMPLETE!", "fontSize": 96, "align": 1, "color": [1.0, 0.85, 0.2, 1] },
            { "type": "Text", "name": "CompleteStats", "anchor": "Center", "offset": [0, 0], "size": [1400, 50],
              "text": "", "fontSize": 44, "align": 1, "color": [1, 1, 1, 1] },
            { "type": "Button", "name": "AgainButton", "anchor": "Center", "offset": [0, 110], "size": [360, 80],
              "text": "Play Again", "fontSize": 40, "color": [0.2, 0.6, 0.3, 1] },
            { "type": "Button", "name": "QuitButton", "anchor": "Center", "offset": [0, 210], "size": [360, 80],
              "text": "Quit", "fontSize": 40, "color": [0.6, 0.2, 0.2, 1] }
          ]
        }
      ]
    }
  ]
}
""")),
        ]),

        new("ui-designer", "ui", "UI Designer", "🎛️",
            "Visual editor for .ghostui layouts, resolutions and menus. Placeholder content.",
            ["designer", "ui editor", "resolution", "menu", "options"],
        [
            B.Note("placeholder", "Placeholder section", "Add screenshots, resolution presets and the Phase 39 options menu (sound, key binds, gamepad binds, video)."),
            B.List("Phase 5: UI Editor Designer, text and 2D layer, widgets, .ghostui runtime", "Phase 20.C / 20.K: main menu, game over, HUD, two players, options, character select", "Phase 38: more resolutions and a restored centered main title", "Phase 39 (planned): sound options, key binds, gamepad binds, video options"),
        ], Placeholder: true),

        // ═════════════════════════  TUTORIALS  ═════════════════════════
        new("build-level-1", "tutorials", "Build Level 1", "🧱",
            "Create the TestGame project, lay out the level, set roles, add an orbit camera and play it.",
            ["tutorial", "level 1", "project", "scene", "startScene", "play"],
        [
            B.H2("l1-steps", "Steps"),
            B.Steps(
                "Create the project|File > New Project. Name it `TestGame` and pick the **Blank** template.",
                "Build the level|Follow the 4C-3 table. Ground, Player, Step, High Platform, 2 Question Blocks, 8 Coins, 3 Enemies, Goal, 1 Directional Light and 1 Point Light.",
                "Configure objects and camera|Use `Ctrl+D` to duplicate. Set each Role. Set `Collider = True` on the Player and every solid object. Add an Orbit Camera to the Player and set it as the Game Start Camera in Scene Settings.",
                "Save the scene|Save as `Scenes/Level1.ghostscene`.",
                "Set the startup scene|Edit `TestGame.ghostproject` and set `startScene`.",
                "Create the HUD|Make `Content/UI/HUD.ghostui` from the JSON in GhostUI Format, or build it in the UI Designer.",
                "Test|Press `Ctrl+P`. Check movement, collisions, coins, enemies, lighting and the Goal."),
            B.Code(B.Tab("TestGame.ghostproject", "json", """
"startScene": "Scenes/Level1.ghostscene",
""")),
            B.Note("warn", "Scene names matter", "The game code expects exactly `Level1.ghostscene` and `Level2.ghostscene`."),
            B.H2("l1-checklist", "Level 1 checklist"),
            B.Check("x|Project created from the Blank template", "x|All required objects added", "x|Object Roles and colliders configured", "x|Orbit Camera assigned as the Game Start Camera",
                    "x|Scene saved as Scenes/Level1.ghostscene", "x|Startup scene configured in TestGame.ghostproject", "x|HUD created and renders", "x|HUD tested, coin collected", "x|Play Mode test", "x|New Window Play Mode test"),
        ]),

        new("build-level-2", "tutorials", "Build Level 2", "🏰",
            "Build the second scene, test the level transition and the win screen, and restore the editor.",
            ["tutorial", "level 2", "transition", "win screen", "goal"],
        [
            B.Steps(
                "Build Level 2|Create `Scenes/Level2.ghostscene` with its own camera and layout.",
                "Test the transition|Open Level1, press `Ctrl+P`, touch the Goal and wait about 2.5 seconds for Level 2 to load.",
                "Test the win screen|Touch Level 2's Goal. **YOU WIN!** appears with Play Again (loads Level 1) and Quit (stops Play mode).",
                "Verify editor restore|Stop with `Shift+F5`. The editor returns to the starting scene, the Hierarchy is restored and the title has no `*`.",
                "Test standalone|Run Play Standalone. Confirm gameplay, the win screen and that Quit closes the window."),
            B.Note("warn", "Level 2 does not load?", "If `Level2.ghostscene` is missing, touching the Goal shows Level Complete and nothing loads. `LoadScene` returns 0 and the transition logic is not yet aware of the final level. Verify the file exists at the expected path. A fallback can be added."),
            B.Check("x|Build Level 2", "x|Test touch Goal of Level 1", "x|Level 2 loads after the Level 1 Goal", "x|Level 2 uses its own camera and layout", "x|Win screen appears after Level 2",
                    "x|Play Again loads Level 1", "x|Quit stops gameplay", "x|Stopping Play restores the editor state", "x|Standalone Play works"),
        ]),

        new("package-game", "tutorials", "Package a Game", "📦",
            "Turn your project into a portable standalone Windows folder (and zip) with the Package Project dialog.",
            ["package", "standalone", "exe", "zip", "release", "pdb", "portable"],
        [
            B.H2("package-steps", "Steps"),
            B.Steps(
                "Configure the startup scene|Set `startScene` in the manifest and save all scenes.",
                "Package|File > Package Project. Leave defaults and click **Package**. The log shows three MSBuild runs, then file count, size, output folder and exe name.",
                "Verify the output|The folder should contain the exe, GhostX.dll, GhostGame.dll, vcruntime DLLs, shaders and Data. No `.pdb` files.",
                "Run it|Use Run Game in the editor or double-click the exe. Title shows the project name, HUD visible, Goal loads Level 2, win screen, Esc quits.",
                "Test portability|Copy the folder elsewhere (ideally another PC) and run it.",
                "Zip|Enable the **Zip** checkbox to create `TestGame.zip` next to the folder.",
                "Package an existing build|Build Release | x64 in Visual Studio, then choose **Use an existing Release x64 folder**. A Debug folder is reported as a mismatch."),
            B.H2("package-layout", "Output layout"),
            B.Code(B.Tab("tree", "text", """
TestGame/
├── TestGame.exe            (renamed GhostShell.exe - plays the game when it finds Data\)
├── GhostX.dll
├── GhostGame.dll
├── GhostShell.dll
├── vcruntime140.dll        (copied from Windows, so no VC++ redistributable install)
├── vcruntime140_1.dll
├── shaders/
└── Data/
    ├── TestGame.ghostproject
    ├── Content/
    └── Scenes/
""")),
            B.H2("auditor", "Audit a package from Java"),
            B.P("A small Java tool can check a packaged folder for the things the checklist asks about: required files present and no `.pdb` files."),
            B.Code(
                B.Tab("Java", "java", """
import java.io.IOException;
import java.nio.file.*;
import java.util.*;
import java.util.stream.*;

/** Usage: java PackageAuditor.java path/to/TestGame TestGame.exe */
public class PackageAuditor {
    static final List<String> REQUIRED = List.of("GhostX.dll", "GhostGame.dll", "shaders", "Data");

    public static void main(String[] args) throws IOException {
        Path root = Path.of(args[0]);
        String exe = args.length > 1 ? args[1] : "Game.exe";
        List<String> problems = new ArrayList<>();

        for (String name : Stream.concat(REQUIRED.stream(), Stream.of(exe)).toList()) {
            if (!Files.exists(root.resolve(name))) problems.add("missing: " + name);
        }
        try (Stream<Path> walk = Files.walk(root)) {
            walk.filter(p -> p.toString().endsWith(".pdb"))
                .forEach(p -> problems.add("debug symbols shipped: " + root.relativize(p)));
        }

        if (problems.isEmpty()) System.out.println("✅ package looks good");
        else { problems.forEach(p -> System.out.println("❌ " + p)); System.exit(1); }
    }
}
"""),
                B.Tab("C#", "csharp", """
// The same audit as a C# one-liner-ish for CI.
var root = args[0];
var problems = new List<string>();
foreach (var req in new[] { "GhostX.dll", "GhostGame.dll", "shaders", "Data" })
    if (!File.Exists(Path.Combine(root, req)) && !Directory.Exists(Path.Combine(root, req)))
        problems.Add($"missing: {req}");

problems.AddRange(Directory.EnumerateFiles(root, "*.pdb", SearchOption.AllDirectories)
                           .Select(p => $"debug symbols shipped: {Path.GetRelativePath(root, p)}"));

Console.WriteLine(problems.Count == 0 ? "✅ package looks good" : string.Join('\n', problems.Select(p => "❌ " + p)));
return problems.Count;
""")),
            B.H2("package-checklist", "Packaging checklist"),
            B.Check("_|All three required projects build successfully", "_|Packaging completes without errors", "_|Required DLLs, shaders and data files are present", "_|No .pdb files are included",
                    "_|Packaged executable launches correctly", "_|Level transitions and HUD work in the packaged game", "_|Portability tested from another folder", "_|ZIP packaging verified", "_|Existing Release x64 folder workflow tested"),
            B.Note("tip", "Expected outcome", "A playable two-level GhostX game with a working HUD, level transitions, a win screen and a packaged Windows executable."),
        ]),

        // ═════════════════════════  ARCHITECTURE  ═════════════════════════
        new("architecture-overview", "arch", "Architecture Overview", "🧬",
            "How the C# shell, the C++ engine DLL and the RHI fit together.",
            ["architecture", "rhi", "pinvoke", "pipeline", "scene model", "dx12"],
        [
            B.P("The editor shell talks to the native engine through P/Invoke. The engine keeps a backend-agnostic scene model, a pipeline that owns PSOs, meshes, textures and offscreen targets, and an RHI interface that the DX12 backend implements."),
            B.Diagram(
                ["GhostShell¦C# WinForms¦Editor UI + DockPanelSuite¦Viewport manager¦Asset library",
                 "GhostX.dll¦C++ engine¦Scene model¦Pipeline¦IGhostRHI",
                 "DirectX 12¦GPU backend¦Swap chains¦Surfaces¦Command lists"],
                ["P/Invoke", "RHI"]),
            B.Table(["Layer", "Responsibility"],
            [
                ["Editor UI", "Palette, hierarchy, Details, docking, menus"],
                ["Viewport manager", "Camera windows and live viewports, up to 4"],
                ["Asset library", "Sends texture pixels to the pipeline"],
                ["Scene model", "Backend-agnostic description of the world"],
                ["Pipeline", "Root signature, PSOs, meshes, textures, offscreen targets"],
                ["IGhostRHI", "Interface that hides the graphics API"],
            ]),
        ]),

        new("project-structure", "arch", "Project Structure", "🗂️",
            "What each source file does across GhostX (C++), GhostShell (C#) and GhostBlazorUI.",
            ["files", "folders", "source", "layout", "tree"],
        [
            B.Code(B.Tab("tree", "text", """
GhostX/
├── GhostX/                        # C++ engine (GhostX.dll)
│   ├── GhostEngine.cpp / .h       # Exported API, camera, picking, input
│   ├── GhostScene.h               # Backend-agnostic scene model
│   ├── GhostPipeline.cpp / .h     # Root signature, PSOs, meshes, textures, offscreen targets
│   ├── GhostRHI_DX12.cpp / .h     # DX12 device, swap chains, surfaces
│   ├── IGhostRHI.h                # RHI interface
│   ├── GhostMath.h                # Vector and matrix math
│   └── shaders/                   # GhostVS.hlsl, GhostPS.hlsl
├── GhostShell/                    # C# WinForms editor
│   ├── GhostForm.cs               # Main window and engine loop
│   ├── GhostEditorUI.cs           # Palette, hierarchy, Details, docking
│   ├── GhostDocking.cs            # Dockable window wrapper, key filter
│   ├── GhostViewports.cs          # Camera windows
│   ├── GhostAssets.cs             # Texture library and thumbnails
│   ├── GhostContentBrowser.cs     # Content Browser panel
│   ├── GhostImageLoader.cs        # Image decoding
│   ├── GhostEngine.cs             # P/Invoke bindings
│   └── GhostMenuBuilder.cs        # Menu bar
└── GhostBlazorUI/                 # Blazor WASM UI (experimental)
""")),
        ]),

        new("rhi-backends", "arch", "RHI Backends", "🎨",
            "DirectX 12 is live. DirectX 11, Vulkan, OpenGL and friends are pending.",
            ["rhi", "directx", "vulkan", "opengl", "backend", "ighostrhi", "renderer"],
        [
            B.Table(["Backend", "Status"],
            [
                ["DirectX 12", "✅ Featured"],
                ["Vulkan", "⏳ Pending (planned for Publish/Release)"],
                ["DirectX 11", "⏳ Pending"],
                ["OpenGL", "⏳ Pending"],
                ["DirectX 9", "⏳ Pending"],
                ["Direct X 4", "⏳ Pending (we will believe it when we see it)"],
            ], filter: true),
            B.H2("rhi-iface", "A backend in one interface"),
            B.Note("placeholder", "Placeholder code", "Illustrative sketch of IGhostRHI. Replace with the real header."),
            B.Code(
                B.Tab("C++", "cpp", """
// IGhostRHI.h  (sketch)
#pragma once
#include <cstdint>

struct GhostSurfaceDesc { void* hwnd; uint32_t width, height; bool vsync; uint32_t msaa; };

class IGhostRHI
{
public:
    virtual ~IGhostRHI() = default;

    virtual bool  Init() = 0;
    virtual void* CreateSurface(const GhostSurfaceDesc& desc) = 0;
    virtual void  Resize(void* surface, uint32_t w, uint32_t h) = 0;

    virtual void  BeginFrame(void* surface, const float clear[4]) = 0;
    virtual void  Draw(uint32_t mesh, uint32_t material, const float world[16]) = 0;
    virtual void  EndFrame(void* surface) = 0;

    virtual void  Shutdown() = 0;
};
"""),
                B.Tab("C#", "csharp", """
// Choosing a backend from the editor (planned, once more than one exists)
public enum RhiBackend { DirectX12, Vulkan, DirectX11, OpenGL }

public static class RhiSelector
{
    public static RhiBackend Current { get; private set; } = RhiBackend.DirectX12;

    public static bool TrySelect(RhiBackend backend) =>
        backend == RhiBackend.DirectX12 && (Current = backend) == backend;   // only DX12 for now
}
""")),
        ]),

        new("capacity", "arch", "Capacity & Limits", "📏",
            "Hard limits for meshes, scenes, cameras and sky parameters.",
            ["limits", "capacity", "vertices", "objects", "cameras"],
        [
            B.Stats("1,000,000|Vertices per mesh", "2,048|Max scene objects", "4|Live camera viewports", "47|Cubemap sky parameters"),
            B.Note("note", "Raised before", "Phase 15.B increased the per-scene caps. Check the roadmap for the latest changes."),
        ]),

        // ═════════════════════════  SCRIPTING & TOOLS  ═════════════════════════
        new("csharp-api", "scripting", "C# API (P/Invoke)", "🔷",
            "Call the native engine from C#: bindings, the update loop and a mod interface sketch.",
            ["csharp", "pinvoke", "dllimport", "api", "mod", "interop"],
        [
            B.Note("placeholder", "Placeholder API surface", "Function names beyond GhostEngine_SetClearColor are illustrative. Replace with the real exports from GhostEngine.cs."),
            B.H2("bindings", "Bindings"),
            B.Code(
                B.Tab("GhostEngine.cs", "csharp", """
using System.Runtime.InteropServices;

public static class GhostEngine
{
    private const string Dll = "GhostX.dll";

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GhostEngine_Init(IntPtr hwnd, int width, int height);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GhostEngine_Frame();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GhostEngine_SetClearColor(float r, float g, float b);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GhostEngine_LoadScene([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GhostEngine_Shutdown();
}
"""),
                B.Tab("Mod interface", "csharp", """
// A custom mod: tint the sky when the player grabs a coin.
public interface IGhostMod
{
    string Name { get; }
    void OnLoad(IGhostEditor editor);
    void OnPlayStart();
    void OnPlayStop();
}

public sealed class CoinFlashMod : IGhostMod
{
    public string Name => "Coin Flash";
    public void OnLoad(IGhostEditor editor) => editor.Log($"{Name} loaded");
    public void OnPlayStart() => GhostGame.Events.CoinCollected += Flash;
    public void OnPlayStop()  => GhostGame.Events.CoinCollected -= Flash;

    private static async void Flash(object? s, EventArgs e)
    {
        GhostEngine.GhostEngine_SetClearColor(0.9f, 0.8f, 0.2f);
        await Task.Delay(80);
        GhostEngine.GhostEngine_SetClearColor(0.02f, 0.05f, 0.12f);
    }
}
"""),
                B.Tab("Gamepad", "csharp", """
// XInput + HID polling sketch (Phase 34)
public readonly record struct PadState(bool Jump, bool Sprint, bool Attack, bool Pipe, float MoveX, float MoveY);

public static class Pad
{
    public static PadState Read(int index = 0)
    {
        // TODO: call XInputGetState / HID report parser
        return default;
    }
}
""")),
        ], Placeholder: true),

        new("java-tooling", "scripting", "Java Tooling", "☕",
            "Companion Java tools: a project validator and a scene scanner that read GhostX JSON files.",
            ["java", "tools", "validator", "ghostproject", "ghostscene", "jackson", "ci"],
        [
            B.P("GhostX project files are plain JSON, which makes them easy to process from Java build scripts and CI. These tools are standalone and do not touch the engine."),
            B.Note("placeholder", "Placeholder tools", "The .ghostscene schema below is illustrative. Adjust field names to match real files."),
            B.Code(
                B.Tab("ProjectValidator.java", "java", """
import com.fasterxml.jackson.databind.*;
import java.nio.file.*;

/** Verifies that startScene in a .ghostproject points at a real scene file. */
public final class ProjectValidator {
    private static final ObjectMapper JSON = new ObjectMapper();

    public static void main(String[] args) throws Exception {
        Path manifest = Path.of(args[0]);                  // e.g. Data/TestGame.ghostproject
        Path dataDir  = manifest.getParent();

        JsonNode root  = JSON.readTree(Files.readString(manifest));
        String   start = root.path("startScene").asText("");

        if (start.isBlank()) fail("startScene is empty");
        if (!Files.exists(dataDir.resolve(start))) fail("startScene not found: " + start);

        for (String required : new String[] { "Scenes/Level1.ghostscene", "Scenes/Level2.ghostscene" }) {
            if (!Files.exists(dataDir.resolve(required))) fail("game code expects " + required);
        }
        System.out.println("✅ project OK -> " + start);
    }

    private static void fail(String msg) { System.err.println("❌ " + msg); System.exit(1); }
}
"""),
                B.Tab("SceneStats.java", "java", """
import com.fasterxml.jackson.databind.*;
import java.nio.file.*;
import java.util.*;
import java.util.stream.*;

/** Counts objects per role in a .ghostscene and warns near the 2048 object cap. */
public final class SceneStats {
    private static final int MAX_OBJECTS = 2048;

    public static void main(String[] args) throws Exception {
        JsonNode scene = new ObjectMapper().readTree(Files.readString(Path.of(args[0])));
        JsonNode objects = scene.path("objects");

        Map<String, Long> byRole = StreamSupport.stream(objects.spliterator(), false)
            .collect(Collectors.groupingBy(o -> o.path("role").asText("None"), TreeMap::new, Collectors.counting()));

        byRole.forEach((role, n) -> System.out.printf("%-14s %d%n", role, n));
        System.out.printf("%ntotal %d / %d%n", objects.size(), MAX_OBJECTS);
        if (objects.size() > MAX_OBJECTS * 0.9) System.out.println("⚠ close to the scene object cap");
    }
}
"""),
                B.Tab("pom.xml", "html", """
<dependency>
  <groupId>com.fasterxml.jackson.core</groupId>
  <artifactId>jackson-databind</artifactId>
  <version>2.17.2</version>
</dependency>
""")),
            B.H2("run-java", "Run them"),
            B.Code(B.Tab("bash", "bash", """
java -cp "jackson-core.jar:jackson-databind.jar:jackson-annotations.jar" ProjectValidator.java Data/TestGame.ghostproject
java -cp "jackson-core.jar:jackson-databind.jar:jackson-annotations.jar" SceneStats.java Data/Scenes/Level1.ghostscene
""")),
        ], Placeholder: true),

        new("shaders-hlsl", "scripting", "Shaders (HLSL)", "🌈",
            "GhostVS.hlsl and GhostPS.hlsl. A minimal lit, textured sketch. Placeholder content.",
            ["hlsl", "shader", "vertex", "pixel", "lighting", "pbr", "gpu"],
        [
            B.Note("placeholder", "Placeholder shaders", "Illustrative only. Replace with the real files from GhostX/shaders."),
            B.Code(
                B.Tab("GhostVS.hlsl", "hlsl", """
cbuffer FrameCB : register(b0)
{
    float4x4 ViewProj;
    float4   LightDir;     // xyz = direction, w = intensity
    float4   LightColor;
};

cbuffer ObjectCB : register(b1)
{
    float4x4 World;
};

struct VSIn  { float3 pos : POSITION; float3 nrm : NORMAL; float2 uv : TEXCOORD0; };
struct VSOut { float4 pos : SV_Position; float3 nrm : NORMAL; float2 uv : TEXCOORD0; };

VSOut main(VSIn v)
{
    VSOut o;
    float4 world = mul(float4(v.pos, 1.0f), World);
    o.pos = mul(world, ViewProj);
    o.nrm = normalize(mul(v.nrm, (float3x3)World));
    o.uv  = v.uv;
    return o;
}
"""),
                B.Tab("GhostPS.hlsl", "hlsl", """
Texture2D    Albedo : register(t0);
SamplerState Samp   : register(s0);

cbuffer FrameCB : register(b0)
{
    float4x4 ViewProj;
    float4   LightDir;
    float4   LightColor;
};

float4 main(float4 pos : SV_Position, float3 nrm : NORMAL, float2 uv : TEXCOORD0) : SV_Target
{
    float3 albedo  = Albedo.Sample(Samp, uv).rgb;
    float  ndotl   = saturate(dot(normalize(nrm), -LightDir.xyz));
    float3 ambient = albedo * 0.12f;
    float3 diffuse = albedo * LightColor.rgb * ndotl * LightDir.w;
    return float4(ambient + diffuse, 1.0f);
}
""")),
        ], Placeholder: true),

        // ═════════════════════════  BLAZOR  ═════════════════════════
        new("blazor-setup", "blazor", "Blazor Setup", "🌐",
            "Host the Blazor WASM UI inside the WinForms DX12 shell through WebView2.",
            ["blazor", "wasm", "webview2", "winforms", "setup", "publish", "program.cs"],
        [
            B.H2("setup-steps", "Setup and installation"),
            B.Steps(
                "Create the Blazor app|Add a new **Blazor WebAssembly** project to the existing Visual Studio solution next to the WinForms project.",
                "Add WebView2|Install `Microsoft.Web.WebView2` in the GhostShell project.",
                "Add GhostUIHost.cs|Add `GhostUIHost.cs` to GhostShell. It manages WebView2 initialization and points at the published Blazor output.",
                "Update Program.cs|Start the UI host before the application runs.",
                "Update GhostForm.cs|Show the WebView2 control and handle layout alongside the DX12 render surface.",
                "Update GhostMenuBuilder.cs|Add menu items that talk to the Blazor UI.",
                "Update index.html|Configure the base href and WebView2 compatibility in `wwwroot/index.html`.",
                "Check both .csproj files|GhostBlazorUI needs the WebAssembly references and target framework. GhostShell needs the WebView2 package reference.",
                "Publish and copy|Publish Blazor, then copy its wwwroot output into the WinForms bin folder.",
                "Build and run|Run GhostShell. The DX12 renderer is active and the Blazor UI is hosted via WebView2."),
            B.Code(
                B.Tab("Program.cs", "csharp", """
internal static class Program
{
    [STAThread]
    static void Main()
    {
        GhostUIHost.Start();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new GhostForm());
    }
}
"""),
                B.Tab("GhostUIHost.cs", "csharp", """
// Placeholder: a minimal static file host for the published Blazor output.
using System.Net;

public static class GhostUIHost
{
    private static HttpListener? _listener;
    public const string Url = "http://localhost:7177/";

    public static void Start()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        _listener = new HttpListener();
        _listener.Prefixes.Add(Url);
        _listener.Start();

        Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                var ctx = await _listener.GetContextAsync();
                var rel = ctx.Request.Url!.AbsolutePath.TrimStart('/');
                var file = Path.Combine(root, rel == "" ? "index.html" : rel);

                if (!File.Exists(file)) file = Path.Combine(root, "index.html");   // SPA fallback
                var bytes = await File.ReadAllBytesAsync(file);
                ctx.Response.ContentType = MimeFor(file);
                await ctx.Response.OutputStream.WriteAsync(bytes);
                ctx.Response.Close();
            }
        });
    }

    private static string MimeFor(string f) => Path.GetExtension(f) switch
    {
        ".html" => "text/html", ".js" => "text/javascript", ".css" => "text/css",
        ".json" => "application/json", ".wasm" => "application/wasm", ".dll" => "application/octet-stream",
        _ => "application/octet-stream"
    };
}
"""),
                B.Tab("publish", "bash", """
dotnet publish GhostBlazorUI -c Debug

# copy the published wwwroot into the WinForms bin folder
# GhostBlazorUI/bin/Debug/net10.0/publish/wwwroot/  ->  GhostShell/bin/Debug/net10.0/wwwroot/
""")),
        ]),

        new("blazor-bridge", "blazor", "Blazor to DX12 Bridge", "🔗",
            "Change the DirectX 12 clear color live from a Blazor button via WebView2 messages.",
            ["bridge", "js", "interop", "webmessage", "clear color", "setcolor", "postmessage"],
        [
            B.P("Once the base setup works you can wire live two-way communication. This example lets the Blazor frontend control the DX12 clear color in real time."),
            B.Diagram(
                ["Blazor Home.razor¦WASM¦ToggleColor()",
                 "ghostBridge¦JavaScript¦postMessage",
                 "WebView2¦GhostForm.cs¦WebMessageReceived",
                 "GhostEngine¦C++ DLL¦SetClearColor"],
                ["JS interop", "WebView2", "P/Invoke"]),
            B.H2("bridge-code", "The three pieces"),
            B.Code(
                B.Tab("index.html", "js", """
// Inside <head>, before blazor.webassembly.js
window.ghostBridge = {
    sendColor: function (colorStr) {
        window.chrome.webview.postMessage("SetColor:" + colorStr);
    }
};
"""),
                B.Tab("Home.razor", "razor", """
@inject IJSRuntime JS

<button @onclick="ToggleColor">Toggle BG Color</button>

@code {
    private bool _toggled = false;

    private async Task ToggleColor()
    {
        _toggled = !_toggled;
        await JS.InvokeVoidAsync("ghostBridge.sendColor",
            _toggled ? "0.2,0.0,0.4" : "0.0,0.0,0.0");
    }
}
"""),
                B.Tab("GhostForm.cs", "csharp", """
// Inside OnToggleGhostUI, after CoreWebView2.Navigate()
_ghostUI.CoreWebView2.WebMessageReceived += (sender, args) =>
{
    var msg = args.TryGetWebMessageAsString();
    if (msg.StartsWith("SetColor:"))
    {
        var parts = msg.Replace("SetColor:", "").Split(',');
        float r = float.Parse(parts[0]);
        float g = float.Parse(parts[1]);
        float b = float.Parse(parts[2]);
        GhostEngine.GhostEngine_SetClearColor(r, g, b);
    }
};
""")),
            B.Note("tip", "Result", "Clicking the toggle in the Blazor UI sends a message through WebView2 straight to `GhostEngine_SetClearColor`, changing the DX12 background in real time."),
            B.Note("warn", "Culture bug waiting to happen", "`float.Parse` uses the current culture. On a machine with a comma decimal separator, parse with `CultureInfo.InvariantCulture`."),
        ]),

        // ═════════════════════════  REFERENCE  ═════════════════════════
        new("roadmap", "ref", "Roadmap", "🗺️",
            "Every phase, shipped and planned. Filter, search and expand each lane.",
            ["roadmap", "phases", "planned", "done", "todo", "progress"],
        [
            B.RoadmapView(),
        ]),

        new("faq", "ref", "FAQ", "💬",
            "Quick answers. The official How To FAQ is still marked Coming Soon, so this is a starter.",
            ["faq", "help", "how to", "questions"],
        [
            B.Note("placeholder", "Placeholder section", "The README lists the FAQ as Coming Soon. These are starter entries drawn from the notes."),
            B.Faq(
                "Why does the editor layout look broken?|Delete the XML file in `%LocalAppData%\\GhostX` and relaunch. Or use Window > Reset Layout.",
                "My FBX looks huge or tiny.|Set Largest Side fit to 2 when importing.",
                "Does moving files break my project?|No crash, but the engine does not track path changes yet. Re-upload the moved assets.",
                "How do I snap to grid?|Set the snap value and Translate in the dropdown, then drag the XYZ arrows (not the object).",
                "Why does AnyCPU Debug fail?|It cannot find the DLL. Use x64 or AnyCPU Release for now.",
                "Is there an installer?|Not yet. There is no Install Shield Wizard for the engine or for built games. Packaging produces a folder with an exe.",
                "How do I add custom roles?|Coming soon. See Object Roles.",
                "Can I use Vulkan?|Planned. DirectX 12 is the only live backend."),
        ], Placeholder: true),

        new("credits-license", "ref", "Credits & License", "🏅",
            "Libraries, resources and the MIT license.",
            ["credits", "license", "mit", "assimp", "dockpanelsuite", "resources"],
        [
            B.Cards(
                new Card("🪟", "DockPanelSuite", "MIT-licensed docking UI that gives the editor its Visual Studio feel."),
                new Card("📥", "Assimp", "Model importing, including FBX."),
                new Card("🔊", "Sound resources", "zophar.net NES music archive and sounds.spriters-resource.com."),
                new Card("🖌️", "Texture resources", "spriters-resource.com.")),
            B.H2("license", "License"),
            B.Code(B.Tab("MIT", "text", """
MIT License

Copyright (c) 2026 robert-robot2

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
""")),
        ]),
    ];

    // ─────────────────────────────────────────────────────────
    //  ROADMAP  (transcribed from the README)
    // ─────────────────────────────────────────────────────────
    static RoadmapItem D(string phase, string title) => new(phase, title, true);
    static RoadmapItem T(string phase, string title) => new(phase, title, false);

    static RoadmapGroup[] BuildRoadmap() =>
    [
        new("Featured / Assimilated", "✅",
        [
            D("1", "Lighting, viewport icons, click/drag, editor panels"),
            D("2", "Textures and materials, free camera entities"),
            D("3", "Dockable editor, live camera viewports, per-camera resolution"),
            D("4", "Project save/load, load in VS, game template, basic UI, play mode, object roles, .ghostproject/.ghostscene, orbit camera"),
            D("5", "UI Editor Designer, text and 2D layer, widgets, .ghostui and runtime"),
            D("6", "Sky sphere, custom mods, drag and drop, cubemap with 47 parameters and custom layers"),
            D("7.A", "Sprite Sheet Editor creator (square)"),
            D("7.B", "Sprite Sheet Animation Editor, .ghostanim files"),
            D("8", "PBR materials layout"),
            D("9", "Object shadows"),
            D("10", "AnyCPU/X64 Debug/Release builds, docking suite XML delete to clear"),
            D("11.A", "FBX imports as default color import (Largest Side fit)"),
            D("11.B", "Saving models, the Models palette and Content Browser tiles"),
            D("11.C", "Materials, collider fit, re-import"),
            D("12.A", "Pre-building prep, scene switching"),
            D("12.B", "Pre-building prep, the standalone exe"),
            D("13.A", "Viewport tools, grid, gizmos and dimensions"),
            D("13.B", "Drag gizmos (translate, rotate, scale), axis constraints, grid snapping"),
            D("13.C", "Click a dimension to type a new size, edge selection, units in Details"),
            D("14", "Viewport added to Window dropdown, post-build copy of GhostGame.dll to x64 Release"),
            D("15", "Test / fix / add / quality control"),
            D("15.A", "Save as, Save, Set Startup Scene, trace stack error for new/open project"),
            D("15.B", "Max caps per scene increased, Play calls scene"),
            D("12.C", "Level 1 build and test play, instructions"),
            D("15.C", "Advanced object roles"),
            D("15.E", "2D animation states (pushed to Phase 16)"),
            D("15.G", "Restore function: projects new/load, GhostContentBrowser constructor fix"),
            D("15.F", "Controls: Q/E/Space for up and down"),
            D("16", "Sprite animation state editor phases"),
            D("19", "Advanced pre-built primitives list"),
            D("15.D", "Skysphere full display without horizon cutoff toggle"),
            D("21", "Per-clip textures for animation states"),
            D("12.E", "UI for playtest on Level 1: HUD, enemies, collision, animation states"),
            D("-", "Critical errors restore: launches in Play Mode standalone window"),
            D("17", "Super Mario Infinitus required: expert roles updated"),
            D("20", "Roles investigated"),
            D("23", "Enemy pathways"),
            D("20.A", "Mario cooked depth changed to 50, power-ups flow on the Z axis"),
            D("20.B", "Block breaking, Journeyman Chaos"),
            D("22", "Sky Sphere night only below axis for underground"),
            D("20.C", "Main menu, game over, HUD, polish"),
            D("20.D", "Patch: coin display for max coins, portal check"),
            D("20.E", "Gameplay Finitus: fireballs, flying enemies, shell drops"),
            D("12.F", "Build the rest of Level 1, test Goal (Instruction Set 1 complete)"),
            D("12.D", "Test: flag pole mesh, goal, scene switching, HUD map (Instruction Set 2)"),
            D("25", "Editor upgrade: camera speed parameter"),
            D("20.H", "Flag pole slide, castle flag raise"),
            D("20.I", "Start screen and carry-over for level change, HUD update for Fire Mario"),
            D("20.J", "Mod upgrade: sky sphere ambience over a selected time period"),
            D("20.K", "Full HUD rework, 2 player, options menu, Luigi character select"),
            D("28", "Editor upgrade: Duplicate in Array, Ctrl+F"),
            D("29.A", "Thicc editor upgrade: red collider box, camera zoom, jump to object, undo/redo, copy/paste, rename, right-click menus, F/G toggles"),
            D("20.L", "Boss"),
            D("29.B", "Thicc editor upgrade part Deuce: camera check, click drag, shift multi-select, F for faces"),
            D("20.M", "Level 2 build (Instruction Set 2 complete)"),
            D("31", "Antialiasing MSAA 2x/4x/8x"),
            D("32", "Antialiasing TAA with jitter adjustment"),
            D("34.A", "Gamepad editor and gameplay, XInput support"),
            D("34.B", "Gamepad menu pad, HID controller support"),
            D("33", "Audio mixer editor, game sounds"),
            D("33.A", "Construct game sounds"),
            D("20.O", "FINAL FORM: flying shell drops, doppelgangers, bopper spinners"),
            D("36", "Patch fix: fireball, Black Olive turtle shell drop enemies animation"),
            D("20.P", "Doppelgangers mesh texture swapping"),
            D("41", "FIREBALL FINAL FORM"),
            D("38", "UI Editor: more resolutions, restore centered main title in main menu"),
            D("29.C", "Thicc editor upgrade part Drei: collision rotation"),
            D("37", "Gamepad zoom (R2 L2), gamepad/keyboard recenter orbit camera (R1 Z)"),
            D("44", "Ghost Engine docs on Roboutes Git Pages"),
            D("35", "Hidden objective"),
        ]),
        new("Active Core", "🔥",
        [
            T("20.G", "Collision check, polish for levels"),
            T("20.P", "Roles and Behaviour Restructure"),
            T("20.N", "Play packaged exe (Instruction Set 3)"),
            T("43", "Base demo gameplay package to download on site without installer"),
       
        ]),
        new("Active Replicant Core", "🧪",
        [
            T("39", "UI FINAL FORM: sound options, key binds, gamepad binds, video options, cleanup"),
            T("40", "Transition audio effects"),
            T("41", "Timed elevators"),
            T("28", "Animations: Mario/Luigi flag slide down, death, transform"),
            T("42", "Thicc editor upgrade part Quattro: resolutions"),
            T("-", "Base engine for use without package installer for download"),
      
        ]),
        new("Active SubCore", "🛰️",
        [    
            T("-", "Install Shield Wizard / packaged install for download"),
            T("-", "Install wizard setup for standalone game"),
            T("-", "Program launcher"),
            T("-", "Download patcher"),
            T("24", "Check all editor windows, move to window palette, add dropdowns"),
            T("26", "Mario dance"),
            T("-", "Levels 1-2 to 8-4, swimming, springs"),
            T("27", "Editor upgrade to quaternion camera"),
            T("-", "Particle editor manager"),
            T("30", "Upgrade roles to show only their behaviours"),
            T("-", "Render polish"),
            T("-", "Antialiasing SMAA / FXAA / TAA"),
            T("-", "Co-op multiplayer"),
            T("-", "Online multiplayer"),
            T("13.D", "CAD-style annotation tools on top of the blueprint grid"),
            T("18", "Player controls editor"),
        ]),
        new("Debug / Errors", "🐞",
        [
            T("Err", "Docking suite debug needs reset / delete xml layout, but why?"),
            T("Err", "AnyCPU cannot find DLL in Debug"),
            T("Err", "Sprite Sheet Editor needs adjustment"),
        ]),
        new("Publish / Release", "📡",
        [
            T("-", "Blazor WASM interactive docs"),
            T("-", "Live updates"),
            T("-", "RHI Vulkan backend"),
        ]),
        new("Pending", "🌌",
        [
            T("-", "Expert primitives"),
            T("-", "Master object roles"),
            T("-", "Advanced Chaos"),
            T("-", "Multi-format mesh import (gltf, obj...), textures, normal/specular maps, shadows"),
            T("-", "Spot light, ray casting, global illumination, bloom, subsurface scattering"),
            T("-", "Shadows: directional light"),
            T("-", "All camera types and features"),
            T("-", "MetaHuman"),
            T("-", "Tile map"),
            T("-", "Shadows: tile map"),
            T("-", "Blueprint material editor, full materials"),
            T("-", "AutoCAD mesh integration, .step import"),
            T("-", "Animation editor, bone hierarchy editor"),
            T("-", "Full mesh editor: sculpting, animation, texture painting"),
            T("-", "Multiple RHI backends mods"),
            T("-", "Interactive Blazor WASM Ghost UI documentation / download / store / mod / updates"),
            T("-", "Live download patcher from the Blazor UI (store lives inside the app UI)"),
            T("-", "Easy mod download library from the Blazor UI dashboard"),
            T("-", "To be added, experimental"),
        ]),
        new("Wildcard", "❓",
        [
            T("-", "Vertical Sync?"),
        ]),
    ];
}
