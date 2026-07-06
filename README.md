# Compost 🌱 - Orchard Core Edition

### Key Advantages
- **Modular Architecture**: Build custom modules for mind maps, kanban boards, meeting transcriptions
- **Content Type System**: Perfect for WorkContext, MindMapNode, TreeNode, KanbanCard as content types
- **Workflow Engine**: Automate the Mind Map → Tree → Kanban promotion flow
- **GraphQL API**: Built-in API for mobile apps or future integrations
- **Admin Dashboard**: Professional UI out of the box with customizable widgets
- **Multi-tenancy**: Future-proof if you want to support teams
- **Responsive Themes**: Works on desktop, tablet (iPad), and mobile (Fold Z6 browser)
- **Real-time with SignalR**: Built-in support for live transcript updates
- **Deployment Flexibility**: Self-host or Azure App Service

## Architecture Overview

```
┌────────────────────────────────────────────────────────────┐
│                    Orchard Core CMS                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   Custom    │  │   Custom    │  │   Custom    │         │
│  │   Modules   │  │   Themes    │  │   Workflows │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         Compost Modules (Your Custom Code)           │  │
│  │  • Compost.Contexts                                  │  │
│  │  • Compost.MindMap                                   │  │
│  │  • Compost.Transcription                             │  │
│  │  • Compost.Kanban                                    │  │
│  │  • Compost.Snippets                                  │  │
│  │  • Compost.Patterns                                  │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Core Services Layer                       │
│  • IContextManager      • IPatternRecognition               │
│  • ITranscription       • ICodeSnippetService               │
│  • IDecomposition       • ISyncService                      │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Data Layer                                │
│  • Cosmos DB (via Orchard's YesSql abstraction)             │
│  • Azure Blob Storage (meetings)                            │
│  • Local SQLite (offline cache)                             │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   External Services                         │
│  • Azure Speech Services  • Azure OpenAI                    │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

```
Compost/
├── src/
│   ├── Compost.Web/                      # Main Orchard Core application
│   │   ├── appsettings.json
│   │   ├── Startup.cs
│   │   └── Program.cs
│   │
│   ├── Modules/                          # Custom Orchard Modules
│   │   ├── Compost.Contexts/            # Context management module
│   │   │   ├── Manifest.cs
│   │   │   ├── Startup.cs
│   │   │   ├── Models/
│   │   │   ├── Controllers/
│   │   │   ├── ViewModels/
│   │   │   ├── Drivers/                 # Content part drivers
│   │   │   ├── Handlers/
│   │   │   ├── Services/
│   │   │   └── Views/
│   │   │
│   │   ├── Compost.MindMap/             # Mind map visualization
│   │   │   ├── Assets/
│   │   │   │   ├── js/mindmap.js       # D3.js or Cytoscape.js
│   │   │   │   └── css/mindmap.css
│   │   │   ├── Models/
│   │   │   ├── Controllers/
│   │   │   │   └── MindMapController.cs
│   │   │   └── Views/
│   │   │       └── MindMap/
│   │   │           └── Index.cshtml
│   │   │
│   │   ├── Compost.Transcription/       # Meeting recording & transcription
│   │   │   ├── Services/
│   │   │   │   ├── AudioRecordingService.cs
│   │   │   │   └── TranscriptionService.cs
│   │   │   ├── Controllers/
│   │   │   │   └── MeetingController.cs
│   │   │   ├── Hubs/                    # SignalR for real-time
│   │   │   │   └── TranscriptionHub.cs
│   │   │   └── Views/
│   │   │
│   │   ├── Compost.Kanban/              # Kanban board
│   │   │   ├── Assets/
│   │   │   │   └── js/kanban.js        # Drag-drop board
│   │   │   ├── Controllers/
│   │   │   └── Views/
│   │   │
│   │   ├── Compost.Snippets/            # Code snippet management
│   │   │   ├── Services/
│   │   │   │   └── SnippetSearchService.cs
│   │   │   ├── Controllers/
│   │   │   └── Views/
│   │   │
│   │   └── Compost.Patterns/            # Architectural patterns
│   │       ├── Services/
│   │       ├── Controllers/
│   │       └── Views/
│   │
│   ├── Themes/                           # Custom themes
│   │   └── Compost.Theme/
│   │       ├── Manifest.cs
│   │       ├── wwwroot/
│   │       │   ├── css/compost.css
│   │       │   └── js/compost.js
│   │       └── Views/
│   │           └── Layout.cshtml
│   │
│   └── Compost.Core/                     # Shared business logic (from original)
│       ├── Models/
│       ├── Interfaces/
│       └── Services/
│
├── tests/
│   └── Compost.Tests/
```

## Content Types in Orchard Core

### WorkContext (Content Type)
```json
{
  "Name": "WorkContext",
  "DisplayName": "Work Context",
  "Parts": [
    { "Name": "TitlePart" },
    { "Name": "AutoroutePart" },
    { "Name": "CommonPart" },
    { "Name": "WorkContextPart" }  // Custom part
  ]
}
```

**WorkContextPart** (Custom Content Part):
- Repository information
- Testing steps
- Open questions
- Time tracking data
- Tags/categories

### MindMapNode (Content Type)
```json
{
  "Name": "MindMapNode",
  "Parts": [
    { "Name": "TitlePart" },
    { "Name": "MarkdownBodyPart" },
    { "Name": "ContainedPart" },      // Part of a WorkContext
    { "Name": "MindMapNodePart" }     // Custom: position, color, connections
  ]
}
```

### TreeNode (Content Type)
```json
{
  "Name": "TreeNode",
  "Parts": [
    { "Name": "TitlePart" },
    { "Name": "MarkdownBodyPart" },
    { "Name": "TreeNodePart" }         // Custom: criteria, dependencies
  ]
}
```

### KanbanCard (Content Type)
```json
{
  "Name": "KanbanCard",
  "Parts": [
    { "Name": "TitlePart" },
    { "Name": "MarkdownBodyPart" },
    { "Name": "KanbanCardPart" }       // Custom: story points, status, checklist
  ]
}
```


### 2. GraphQL API
Built-in API for:
- Mobile app access (future)
- External integrations
- Headless CMS mode

### 3. Indexing & Search
Lucene/Elasticsearch integration for:
- Code snippet search
- Pattern search
- Full-text search across contexts

### 4. Media Library
Store and manage:
- Meeting recordings
- Voice samples
- Screenshots/diagrams

### 5. Widgets & Layers
Dashboard composition:
- Active context widget
- Time tracking widget
- Recent meetings widget
- Quick capture widget

### 6. Admin Dashboard
Professional interface with:
- Content management
- User settings
- Module configuration
- Analytics

### Desktop Browser
- Full featured experience
- Side-by-side panels
- Keyboard shortcuts
- Mouse/trackpad optimized

### Tablet (iPad)
- Responsive layout
- Touch-optimized controls
- Gesture support
- Can pin as web app

### Mobile (Fold Z6)
- Stacked layout when folded
- Side-by-side when unfolded
- Touch gestures
- Optimized for screen size

### PWA Mode
- Install as "app"
- Offline support
- Push notifications
- App-like experience

## Technology Stack

### Frontend
- **Orchard Core CMS 1.8+**
- **Razor Pages / MVC**
- **Vue.js or Alpine.js** (for reactive components)
- **D3.js / Cytoscape.js** (mind map visualization)
- **Sortable.js** (drag-drop kanban)
- **Prism.js** (code syntax highlighting)
- **SignalR** (real-time transcription)

### Backend
- **ASP.NET Core 8.0**
- **YesSql** (Orchard's document database abstraction over SQL/Cosmos)
- **Azure Speech Services**
- **Azure OpenAI**
- **Azure Blob Storage**

### Data Storage
- **Cosmos DB** (via YesSql)
- **SQL Server** (alternative for dev)
- **Blob Storage** (audio recordings)
- **IndexedDB** (browser offline cache)

### Deployment
- **Azure App Service**
- **Docker**
- **CDN** for static assets
- **Application Insights** for monitoring

