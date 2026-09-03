# MASTER PROMPT — Windows Pixabay Wallpaper Manager

Act as a **senior software architect and principal .NET/WPF engineer**.

Design and implement a production-quality Windows desktop application called **Pixabay Wallpaper Manager**.

The application is an intelligent wallpaper management and rotation system that uses the **Pixabay API** to discover, download, cache, select, and automatically apply wallpapers across Windows monitors and, where officially supported by Windows, virtual desktops and the lock screen.

The application should feel like a polished **Windows 11-native application**, while supporting both **Windows 10 and Windows 11 x64**.

Do not treat this as a simple wallpaper downloader. Architect it as a **wallpaper orchestration and recommendation engine** with clear separation between:

```text
Image Providers
      ↓
Search / Recommendation Engine
      ↓
Image Cache / History
      ↓
Wallpaper Targets
      ↓
Wallpaper Groups
      ↓
Wallpaper Application
      ↓
Scheduler
```

---

## 1. Platform and Technology

Target:

- Windows 10 x64
- Windows 11 x64
- .NET 10
- C#
- WPF

Use modern .NET and WPF practices.

The application must be distributed as a **portable/self-contained application**.

The application should not require an installer to run.

However, provide application actions/settings for:

- Start with Windows
- Start minimized to system tray
- Remove Windows startup entry
- Start application manually
- Exit application

Use appropriate Windows mechanisms for startup registration.

Do not require administrator privileges unless Windows explicitly requires them for a specific feature.

---

## 2. Architecture

Use a clean, modular architecture.

Prefer:

- MVVM
- Dependency Injection
- SOLID principles
- Interfaces for platform-specific services
- Async/await
- CancellationToken
- Dependency inversion
- Testable services
- Separation between UI and business logic

Suggested logical architecture:

```text
PixabayWallpaper
│
├── Application
│   ├── Commands
│   ├── Services
│   └── Orchestration
│
├── Domain
│   ├── Models
│   ├── Enums
│   └── Interfaces
│
├── Infrastructure
│   ├── Pixabay
│   ├── Persistence
│   ├── Windows
│   ├── Networking
│   └── Logging
│
├── Presentation
│   ├── Views
│   ├── ViewModels
│   └── Resources
│
└── Tests
```

Do not put business logic in code-behind.

---

## 3. Core Domain Model

The most important architectural concept is the **Wallpaper Target / Group** system.

The application must discover the locations where wallpapers can be applied and expose them to the user.

Examples of targets:

```text
Monitor 1
Monitor 2
Monitor 3
Virtual Desktop 1
Virtual Desktop 2
Windows Lock Screen
```

Depending on what Windows officially exposes, targets may include additional supported destinations.

---

## 4. Wallpaper Groups

Users can group multiple wallpaper targets together.

A group receives **one wallpaper**, and every target in that group receives the same wallpaper.

Example:

```text
Group: Work

Targets:
    Monitor 1
    Monitor 2
```

Result:

```text
Monitor 1 ─┐
           ├── Wallpaper A
Monitor 2 ─┘
```

Another group could be:

```text
Group: Personal

Targets:
    Monitor 3
    Virtual Desktop 2
```

Each group has its own wallpaper rotation configuration.

The UI must allow users to:

- Create groups
- Rename groups
- Delete groups
- Add targets to groups
- Remove targets from groups
- Move targets between groups
- Enable/disable groups
- Configure tags per group
- Configure fallback tags
- Configure schedule per group where appropriate

A target should not unintentionally belong to multiple active groups unless Windows requires such behavior. Validate conflicting configurations.

---

## 5. Target Discovery

At application startup, inspect the current Windows environment and discover available wallpaper targets.

At minimum detect:

- Physical monitors
- Monitor names/identifiers
- Monitor resolution
- Monitor orientation
- Primary monitor
- Monitor position
- DPI/scaling information where available
- Windows virtual desktops where officially accessible
- Lock screen capability/status where officially accessible

Represent these internally using stable identifiers where possible.

Do not rely solely on display numbering such as "Monitor 1", because monitor ordering can change.

Use stable identifiers where Windows provides them.

---

## 6. Multiple Monitors

Support:

- Multiple monitors
- Different resolutions
- Different aspect ratios
- Portrait monitors
- Landscape monitors
- Different DPI/scaling
- Mixed monitor configurations

Example:

```text
Monitor A: 3840 × 2160 landscape
Monitor B: 2560 × 1440 landscape
Monitor C: 1080 × 1920 portrait
```

The application must understand the geometry of each target.

---

## 7. Wallpaper Groups and Image Selection

Each wallpaper group has its own configuration.

Example:

```text
Group: Main Displays

Tags:
    Nature
    Mountains
    Forests

Fallback Tags:
    Landscape
    Scenery
```

When a scheduled wallpaper change occurs:

1. Determine the group.
2. Select one active group tag randomly.
3. Search Pixabay using that tag.
4. Apply the intelligent image-selection algorithm.
5. Download the selected image if necessary.
6. Add it to the local cache/history.
7. Apply it to every target belonging to that group.
8. Record the result.
9. Schedule the next change.

If the group has no tags configured:

```text
Use global fallback tags.
```

If a selected tag produces no usable result:

```text
Try another configured tag.
```

If all configured tags fail:

```text
Use fallback tags.
```

If everything fails:

```text
Use a suitable image from the local cache.
```

Never leave the user without a usable wallpaper merely because Pixabay is temporarily unavailable.

---

## 8. Tag System

Support:

### Group-specific tags

Each group can have its own tags.

Example:

```text
Work:
    architecture
    minimal
    technology

Bedroom:
    nature
    mountains
    sunset
```

### Global fallback tags

Provide a global fallback list:

```text
nature
landscape
abstract
```

Fallback tags are used when:

- A group has no tags.
- Group searches return no usable results.
- Network/API failures occur and a new search is required.

Allow users to:

- Add tags
- Remove tags
- Enable/disable tags
- Reorder tags

Initially use random tag selection.

Architect the system so weighted tag selection can be added later.

---

## 9. Wallpaper Rotation

The primary behavior is:

> A new wallpaper is selected only when the current wallpaper expires.

Do not continuously search/download images.

Supported schedules should include:

```text
Disabled
15 minutes
30 minutes
1 hour
2 hours
4 hours
6 hours
12 hours
Daily
Custom interval
Specific time of day
```

Use a robust background scheduler.

The scheduler must:

- Survive the application window being closed/minimized.
- Continue while the app is in the system tray.
- Avoid blocking the UI.
- Persist the next scheduled execution.
- Handle sleep/resume.
- Handle system clock changes gracefully.
- Avoid duplicate concurrent wallpaper-change operations.

Prevent race conditions such as two scheduler executions downloading/applying wallpapers simultaneously.

---

## 10. Pixabay API Integration

Create a provider abstraction:

```csharp
public interface IImageProvider
{
    Task<IReadOnlyList<ImageResult>> SearchAsync(
        ImageSearchOptions options,
        CancellationToken cancellationToken);
}
```

Implement:

```text
PixabayImageProvider
```

Pixabay endpoint:

```text
https://pixabay.com/api/
```

Do not hard-code the API key.

The user enters their own Pixabay API key.

Store the API key securely using an appropriate Windows mechanism such as:

- Windows Credential Manager
- DPAPI-protected storage

Never:

- Put the API key in source code.
- Put the API key in Git.
- Log the API key.
- Include the API key in exception messages.

---

## 11. Pixabay Parameters

Expose all relevant Pixabay search parameters in the UI.

Support:

```text
key
q
lang
id
image_type
orientation
category
min_width
min_height
colors
editors_choice
safesearch
order
page
per_page
```

### q

URL-encoded search term.

Maximum 100 characters.

### lang

Support:

```text
cs
da
de
en
es
fr
id
it
hu
nl
no
pl
pt
ro
sk
fi
sv
tr
vi
th
bg
ru
el
ja
ko
zh
```

Default:

```text
en
```

### image_type

```text
all
photo
illustration
vector
```

Default:

```text
all
```

### orientation

```text
all
horizontal
vertical
```

Default should be intelligently selected based on target configuration where possible.

### category

Support:

```text
backgrounds
fashion
nature
science
education
feelings
health
people
religion
places
animals
industry
computer
food
sports
transportation
travel
buildings
business
music
```

### min_width

Default should be determined intelligently based on target resolution, but allow manual configuration.

### min_height

Default should be determined intelligently based on target resolution, but allow manual configuration.

### colors

Support:

```text
grayscale
transparent
red
orange
yellow
green
turquoise
blue
lilac
pink
white
gray
black
brown
```

Allow multiple values where Pixabay supports them.

### editors_choice

Support:

```text
true
false
```

### safesearch

Support:

```text
true
false
```

Default:

```text
true
```

### order

Support:

```text
popular
latest
```

### page

Support pagination.

### per_page

Support Pixabay's allowed range:

```text
3-200
```

---

## 12. Intelligent Image Recommendation Engine

Do not simply select the first Pixabay result.

Create a dedicated recommendation component.

For example:

```csharp
IImageRecommendationService
```

The recommendation engine should score candidate images using factors such as:

- Target resolution
- Image dimensions
- Aspect ratio
- Orientation
- Requested image type
- User-selected colors
- Editor's Choice
- Pixabay popularity
- Image quality
- Whether the image has already been used
- Whether the image is already cached
- Whether the image is a favorite
- Whether the image matches the target aspect ratio
- Whether the image would require excessive cropping

Example conceptual scoring:

```text
Resolution compatibility   25%
Aspect ratio compatibility 25%
Image quality              15%
Search relevance           15%
Popularity                 10%
Editor’s Choice             5%
Novelty                     5%
```

Make the scoring weights configurable internally rather than hard-coded throughout the application.

Do not claim that Pixabay provides a semantic relevance score if it does not.

---

## 13. Target-Aware Image Selection

The recommendation engine must consider the target/group geometry.

For example:

```text
Target:
3840 × 2160
```

Prefer images that are:

- At least 3840×2160 where practical.
- Close to a 16:9 aspect ratio.
- Landscape-oriented.

For:

```text
1080 × 1920
```

Prefer:

- Portrait images.
- Appropriate resolution.
- Similar aspect ratio.

Avoid selecting a 16:9 landscape image for a 9:16 portrait display unless no better image is available.

If the group contains monitors with substantially different aspect ratios, choose a strategy that minimizes visual degradation across the entire group.

---

## 14. Image Cropping / Scaling

Implement intelligent wallpaper rendering.

Support Windows wallpaper styles such as:

```text
Fill
Fit
Stretch
Tile
Center
Span
```

Prefer **Fill** by default.

When an image and target have different aspect ratios:

- Preserve image proportions.
- Crop intelligently rather than distort.
- Avoid unnecessary stretching.
- Keep the important central area visible where possible.

Architect this through an image-rendering abstraction rather than mixing it with wallpaper assignment.

---

## 15. Group Image Semantics

A group gets one logical wallpaper.

However, when necessary, the application may generate target-specific rendered versions of the same source image.

Example:

```text
Pixabay source image
        ↓
     Group image
        ↓
 ┌──────┴─────────┐
 ↓                ↓
Monitor A       Monitor B
3840×2160       1080×1920
```

Both monitors are conceptually showing the same group wallpaper, but rendering/cropping may differ if required.

Preserve the original downloaded image.

---

## 16. Local Image Cache

Implement a persistent local cache.

Default location:

```text
%LOCALAPPDATA%\PixabayWallpaper\Wallpapers
```

Allow users to configure the location.

Use the Pixabay image ID as part of the filename where available.

Example:

```text
pixabay_123456789.jpg
```

The cache must prevent unnecessary duplicate downloads.

Before downloading:

1. Check database.
2. Check local file.
3. Verify file exists and is readable.
4. Re-download only when necessary.

---

## 17. Persistent Database

Use SQLite for application data.

Store at least:

```text
Images
---------
PixabayId
SourceUrl
PreviewUrl
LargeImageUrl
LocalPath
SearchTerm
Resolution
Width
Height
ImageType
Orientation
DownloadedAt
FirstUsedAt
LastUsedAt
TimesDisplayed
IsFavorite
Rating
```

Also maintain:

```text
Groups
Targets
GroupTags
FallbackTags
Schedules
Settings
WallpaperApplications
```

Use migrations/versioning so the database schema can evolve.

---

## 18. Wallpaper History

Provide a dedicated History page.

Display:

- Thumbnail
- Pixabay ID
- Search term
- Resolution
- Download date
- Last used
- Number of times displayed
- Favorite status
- Rating

Actions:

```text
Apply
Open image
Open containing folder
Delete
Favorite
Unfavorite
Rate
```

Allow filtering and sorting.

---

## 19. Favorites and Ratings

Implement favorites as a first-class feature.

Users can:

```text
Favorite
Unfavorite
Rate 1-5 stars
```

Favorites should never be automatically deleted by cache cleanup.

Use ratings in the architecture so future versions can use them for recommendation improvements.

For the initial version, ratings do not need to train an AI model.

A deterministic recommendation system is sufficient.

---

## 20. Storage Management

Do not automatically delete images by default.

Keep downloaded images indefinitely unless the user explicitly performs cleanup.

Provide a manual action:

```text
Delete unused wallpapers
```

"Unused" should mean wallpapers that are not:

- Currently active
- Favorites
- Required by history rules

Show before deletion:

```text
127 unused images
4.8 GB
```

Ask for confirmation before deleting.

Also provide:

```text
Delete selected
Delete all non-favorites
Clear cache
```

Never silently delete user data.

---

## 21. Network Failure Behavior

If Pixabay cannot be reached:

1. Retry automatically.
2. Number of retries is configurable.
3. Default retries: **3**.
4. Use exponential backoff.
5. Do not hammer the API.
6. If retries fail, use a suitable local cached image.
7. Keep the scheduler alive.
8. Try again on the next scheduled rotation.

Handle:

- No Internet
- DNS failure
- Timeout
- HTTP 400
- HTTP 401
- HTTP 403
- HTTP 429
- HTTP 5xx
- Invalid API key
- Empty results
- Invalid image URL
- Download failure

Expose understandable messages to the user.

---

## 22. Pixabay Rate Limiting

Use responsible API usage.

Implement:

- Caching
- Duplicate prevention
- Request throttling
- Retry/backoff
- Pagination only when necessary
- No unnecessary polling

Do not continuously query Pixabay while waiting for the next wallpaper.

---

## 23. Search Preview

This is a secondary feature but should be implemented.

Allow users to test their current Pixabay configuration.

Example:

```text
[Test Search]
```

Display a grid of results.

Each result should show:

- Thumbnail
- Resolution
- Orientation
- Image type
- Pixabay ID
- Relevant metadata

Actions:

```text
Preview
Download
Apply
Favorite
```

---

## 24. Manual Wallpaper Control

Provide:

```text
New Wallpaper
Apply Selected
Previous Wallpaper
Pause Rotation
Resume Rotation
```

"New Wallpaper" should immediately perform the same selection process used by the scheduler.

It must still respect:

- Tags
- Filters
- History
- Duplicate prevention
- Target/group configuration
- Image recommendation

---

## 25. System Tray

The application should start minimized to the system tray.

The main application window should not appear automatically unless configured by the user.

Tray menu:

```text
Open
New Wallpaper
Pause Rotation
Resume Rotation
Next Wallpaper
Settings
History
Exit
```

Display notifications for important events.

Examples:

```text
New wallpaper applied.
```

or:

```text
Pixabay unavailable. Using cached wallpaper.
```

---

## 26. Windows Startup

Provide:

```text
☑ Start with Windows
☑ Start minimized to tray
```

Allow the user to enable/disable these options from Settings.

The application should be able to create/remove its startup entry without requiring administrator privileges.

---

## 27. Windows Lock Screen

Lock-screen support is a **Must Have**.

However, implement it according to what Windows officially permits.

Use documented Windows APIs/mechanisms where possible.

Third-party dependencies are acceptable if necessary.

Do not fake support.

If Windows restricts a requested behavior:

- Detect the limitation.
- Explain it clearly.
- Use the closest supported mechanism.
- Keep the feature isolated behind:

```csharp
ILockScreenService
```

The application must never report success when Windows rejected the operation.

---

## 28. Windows Virtual Desktops

Virtual desktop support is a **Should Have**.

Attempt to support it using supported Windows APIs or reputable Windows libraries.

Create:

```csharp
IVirtualDesktopService
```

and:

```csharp
IVirtualDesktopWallpaperService
```

If per-virtual-desktop wallpapers are officially possible on the target Windows version, implement them.

If not:

- Detect the limitation.
- Clearly communicate it.
- Provide the closest reliable behavior.

Do not depend on undocumented Windows internals unless the feature is explicitly marked experimental.

---

## 29. Multiple Monitor Wallpaper Application

Create:

```csharp
IMonitorService
IWallpaperService
```

The wallpaper service should understand:

- Target monitor
- Target resolution
- Orientation
- DPI
- Wallpaper style
- Group membership

Windows-specific APIs should remain isolated from the rest of the application.

---

## 30. Application Dashboard

Create a modern dashboard.

It should show:

```text
Current wallpaper
Current groups
Connected monitors
Rotation status
Next wallpaper change
Cache size
Images downloaded
Images displayed
```

Example:

```text
┌──────────────────────────────────────────────┐
│ Pixabay Wallpaper Manager                    │
├──────────────────────────────────────────────┤
│                                              │
│ Current Wallpaper                            │
│                                              │
│        ┌──────────────────────────┐          │
│        │                          │          │
│        │       PREVIEW            │          │
│        │                          │          │
│        └──────────────────────────┘          │
│                                              │
│ Group: Main Displays                         │
│ Tags: Nature, Mountains                      │
│                                              │
│ Next change: 42 minutes                      │
│                                              │
│ [ New Wallpaper ]  [ Pause ]                │
│                                              │
├──────────────────────────────────────────────┤
│ Dashboard | Groups | History | Search        │
│ Settings                                     │
└──────────────────────────────────────────────┘
```

---

## 31. Groups UI

Create a visual group-management interface.

Example:

```text
Groups

┌─────────────────────────────────────┐
│ Main Displays                        │
│                                     │
│ 🖥 Monitor 1                         │
│ 🖥 Monitor 2                         │
│                                     │
│ Tags: Nature, Mountains, Forest     │
│ Rotation: Every hour                │
│                                     │
│ [Edit] [Pause]                      │
└─────────────────────────────────────┘
```

Provide drag-and-drop or equivalent controls for assigning targets to groups where practical.

---

## 32. Modern Windows UI

The application should have a polished Windows 11-inspired appearance.

Use:

- Fluent design principles
- Dark theme
- Light theme
- System theme
- Mica/Acrylic where appropriate and supported
- Modern navigation
- Responsive layouts
- High-DPI support
- Accessible controls
- Keyboard navigation

Do not sacrifice stability just to achieve visual effects.

The application must still look reasonable on Windows 10.

---

## 33. Settings

Organize settings into clear sections:

```text
General
Appearance
Groups
Interests
Pixabay
Wallpaper
Monitors
Virtual Desktops
Lock Screen
Schedule
Startup
Notifications
Storage
Advanced
About
```

Avoid overwhelming the user with every technical setting on the main page.

Use sensible defaults.

---

## 34. Global Defaults

Use sensible defaults such as:

```text
Safe Search: enabled
Pixabay order: popular
Image type: all
Orientation: automatic
Automatic rotation: enabled
Rotation: every hour
Start with Windows: disabled until user enables it
Start minimized to tray: enabled
Cache deletion: manual
Retry count: 3
Theme: system
```

Do not make destructive operations automatic.

---

## 35. Logging

Implement structured logging.

Log:

- Application startup/shutdown
- Scheduler operations
- Pixabay requests without secrets
- API failures
- Image selection
- Downloads
- Cache operations
- Wallpaper application
- Windows API failures
- Virtual desktop operations
- Lock-screen operations
- Unexpected exceptions

Never log:

```text
API keys
Credentials
Sensitive user information
```

Provide a user-accessible log folder.

---

## 36. Error Handling

The application must be resilient.

A failure in one group must not crash the entire application.

For example:

```text
Group A → successful
Group B → Pixabay failure → cached image
Group C → monitor unavailable → log and retry
```

Continue processing other groups.

Avoid unhandled background exceptions.

---

## 37. Sleep / Resume

Handle Windows sleep and resume.

When the computer resumes:

- Re-evaluate the scheduler.
- Determine whether a wallpaper rotation was missed.
- Avoid performing dozens of missed rotations.
- Optionally perform one rotation if the scheduled time has passed.
- Recalculate the next scheduled execution.

---

## 38. Application Lifecycle

The application should behave correctly when:

- Started
- Minimized
- Restored
- Closed
- Running in tray
- Windows logs off
- Windows shuts down
- Computer sleeps
- Computer resumes
- Network disconnects
- Network reconnects
- Monitor is connected/disconnected

---

## 39. Monitor Changes

Handle dynamic monitor changes.

For example:

```text
Laptop monitor
+
External monitor connected
```

The application should detect the change and refresh the target list.

Likewise:

```text
External monitor disconnected
```

The application should gracefully remove/update the target without corrupting group configuration.

---

## 40. Concurrency

Ensure that wallpaper operations are serialized per group.

Example:

```text
Group A:
    Rotation running → another request arrives
```

Do not run two wallpaper changes simultaneously for the same group.

Use appropriate locking/semaphores.

Different independent groups may operate concurrently where safe.

---

## 41. Extensibility

Pixabay must not be hard-coded throughout the application.

Use:

```csharp
IImageProvider
```

so future providers can be added.

Potential future providers:

```text
Pixabay
Unsplash
Pexels
Local Folder
Network Folder
```

The current implementation only needs Pixabay.

---

## 42. Data and Settings Export

Implement import/export architecture even if the UI is initially simple.

Users should eventually be able to export:

- Groups
- Tags
- Settings
- Preferences

Do not export the API key in plaintext.

If the export contains sensitive information, explicitly warn the user.

---

## 43. Privacy

The application should not collect analytics or telemetry by default.

Do not upload:

- User settings
- Wallpaper history
- Local paths
- Usage statistics

to any external service.

The only external service required by the core application should be Pixabay.

---

## 44. Performance

The application should have low background resource usage.

Avoid:

- Constant polling
- Excessive API calls
- Loading full-resolution images into memory unnecessarily
- Blocking the UI thread
- Repeated database scans

Use asynchronous operations.

Use thumbnails for UI galleries.

Load full-resolution images only when necessary.

---

## 45. Image Validation

Before an image becomes an active wallpaper:

- Verify download completed.
- Verify file exists.
- Verify file is readable.
- Verify image dimensions.
- Verify supported image format.
- Verify it is not corrupted.

If validation fails:

```text
discard candidate
try another candidate
```

Do not apply corrupt files.

---

## 46. Security

Follow secure coding practices.

Protect:

- API key
- Local configuration
- Database
- File paths

Validate external URLs before downloading.

Use HTTPS.

Do not blindly trust downloaded files.

Do not execute downloaded content.

---

## 47. Unit Tests

Provide tests for core logic.

At minimum test:

```text
Pixabay query generation
Pixabay parameter validation
Tag selection
Fallback tag selection
Image recommendation scoring
Aspect ratio matching
Duplicate detection
Cache behavior
History behavior
Scheduler calculations
Retry logic
Group assignment
Monitor target handling
```

Windows-specific functionality should be abstracted so the majority of business logic can be tested without requiring a live Windows desktop environment.

---

## 48. Integration Tests

Where practical, provide integration tests for:

- Pixabay API client
- Database persistence
- Image download
- Cache behavior

Do not require a real API key for ordinary unit tests.

Use mocked/fake providers.

---

## 49. README

Provide a complete README covering:

### Installation

Explain how to run the portable application.

### Pixabay API Key

Explain how the user obtains and configures their own API key.

### Configuration

Explain groups, tags, schedules, monitors, cache, and wallpaper behavior.

### Windows Startup

Explain how startup registration works.

### Lock Screen

Explain supported Windows behavior and limitations.

### Virtual Desktops

Explain supported behavior and limitations.

### Storage

Explain where wallpapers/database/logs are stored.

### Development

Explain:

```text
Prerequisites
Build
Run
Test
Publish
```

---

## 50. Publishing

The application should be publishable as a self-contained portable Windows x64 application.

Prefer a command equivalent to:

```text
dotnet publish -c Release -r win-x64 --self-contained true
```

The exact publish configuration should be appropriate for .NET 10.

Do not require the end user to install the .NET runtime.

---

## 51. Configuration Philosophy

Use sensible defaults so a new user can get started quickly.

Initial setup should ideally be:

```text
1. Enter Pixabay API key
2. Add interests
3. Configure groups
4. Choose rotation interval
5. Enable startup if desired
6. Done
```

The advanced configuration should remain available without making it mandatory.

---

## 52. Recommended First-Run Experience

On first launch:

```text
Welcome to Pixabay Wallpaper Manager

1. Pixabay API Key
   [________________________]

2. What are you interested in?
   [ nature ]
   [ mountains ]

3. Wallpaper rotation
   [ Every hour ▼ ]

4. Where should wallpapers be applied?
   ☑ Monitor 1
   ☑ Monitor 2

5. Start with Windows
   ☐

[Finish Setup]
```

Automatically create a default group such as:

```text
Main Displays
```

and assign the selected monitors to it.

---

## 53. Application States

Model wallpaper state explicitly.

For example:

```text
Idle
Searching
Downloading
Validating
Applying
Applied
Retrying
UsingCache
Failed
Paused
```

Expose meaningful states to the UI.

Example:

```text
Downloading new wallpaper...
```

rather than freezing the interface.

---

## 54. Important Architectural Rule

Separate these concepts:

```text
Image
Image Candidate
Downloaded Image
Wallpaper
Wallpaper Target
Wallpaper Group
Wallpaper Assignment
```

They are not the same entity.

For example:

```text
Pixabay image
     ↓
Downloaded image
     ↓
Selected wallpaper
     ↓
Group assignment
     ↓
Monitor-specific rendering
     ↓
Windows wallpaper
```

This separation is critical for history, caching, recommendations, multiple monitors, and future providers.

---

## 55. Avoid Overengineering

Do not introduce unnecessary complexity.

Do not add:

- AI/LLM services
- Cloud backend
- User accounts
- Remote server
- Telemetry
- Microservices
- Web backend

The application should be a **local-first Windows desktop application**.

Use SQLite and local services.

Pixabay is the external dependency.

---

## 56. Priority

Implement features according to this priority.

### Must Have

1. Pixabay integration
2. Multiple tags
3. Group-specific tags
4. Fallback tags
5. Automatic rotation
6. Random tag selection
7. Random image selection
8. Intelligent image recommendation
9. Image history
10. Local cache
11. System tray
12. Windows startup
13. Lock screen support where Windows allows it
14. Advanced Pixabay filters
15. Storage management
16. Multiple-monitor foundation
17. Secure API-key storage
18. Modern WPF UI
19. Robust network failure handling
20. Persistent database

### Should Have

1. Search preview
2. Manual wallpaper change
3. Multiple monitors
4. Different wallpaper rendering per monitor
5. Virtual desktops
6. Favorites
7. Ratings
8. Monitor hot-plug detection
9. Sleep/resume handling

### Nice to Have

1. Statistics
2. Advanced recommendation learning
3. Additional image providers
4. Import/export
5. Advanced automation rules

---

## 57. Implementation Strategy

Do not attempt to write the entire application as one giant file.

Implement it in logical stages.

Recommended order:

### Phase 1 — Foundation

- Solution
- Projects
- MVVM
- DI
- Logging
- Configuration
- Database

### Phase 2 — Pixabay

- API client
- Search models
- Parameter validation
- Search service
- API error handling

### Phase 3 — Image Engine

- Image download
- Cache
- History
- Duplicate detection
- Recommendation engine

### Phase 4 — Windows Wallpaper

- Monitor discovery
- Wallpaper service
- Image rendering
- Multi-monitor support

### Phase 5 — Groups

- Targets
- Groups
- Tags
- Fallback tags
- Group-specific schedules

### Phase 6 — Scheduler

- Automatic rotation
- Retry
- Sleep/resume
- Tray integration

### Phase 7 — Windows Integration

- Startup
- Notifications
- Lock screen
- Virtual desktops

### Phase 8 — UI Polish

- Fluent design
- Dark/light/system themes
- Dashboard
- History
- Search
- Settings
- Groups

### Phase 9 — Testing

- Unit tests
- Integration tests
- Failure scenarios
- Monitor changes
- Network failures

---

## 58. Final Engineering Requirements

The final solution must be:

- Buildable
- Runnable
- Maintainable
- Testable
- Modular
- Production-oriented
- Portable
- Secure
- Responsive
- Resilient to network failures

Do not provide pseudocode where actual implementation is reasonably possible.

Do not invent Windows capabilities.

When a Windows feature has limitations, explicitly identify the limitation and implement the most reliable supported behavior.

Do not hide unsupported functionality behind UI controls that falsely imply it works.

Use documented Windows APIs wherever possible.

For third-party dependencies, choose mature, actively maintained libraries and explain why each dependency is needed.

---

## 59. Final Deliverable

Produce the complete implementation with:

- Solution/project structure
- Source code
- XAML
- ViewModels
- Models
- Services
- Database layer
- Pixabay client
- Image recommendation engine
- Cache
- History
- Scheduler
- Windows wallpaper integration
- Monitor discovery
- Group management
- System tray
- Startup integration
- Lock-screen integration where supported
- Virtual-desktop integration where supported
- Logging
- Error handling
- Tests
- README
- Publish configuration

The result should be a **real, buildable Windows application**, not a conceptual prototype.

Before implementing platform-specific functionality, verify the actual capabilities and limitations of Windows 10/11 and .NET 10.

When a requirement conflicts with a Windows limitation, prioritize:

```text
Correctness
    >
Reliability
    >
Supported Windows APIs
    >
Maintainability
    >
Feature completeness
```

The application should feel like a polished commercial-quality Windows utility rather than a proof of concept.
