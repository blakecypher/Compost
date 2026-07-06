# Compost - Orchard Core Quick Start Guide

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB, Express, or full version) OR PostgreSQL
- Visual Studio 2022 / VS Code / Rider
- Node.js 18+ (for frontend assets)

### Initial Setup

#### 1. Clone and Restore
```bash
cd Compost-OrchardCore
dotnet restore
```

#### 2. Configure Database

**Option A: SQL Server LocalDB (Windows)**
```json
// appsettings.json already configured for LocalDB
"ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=Compost;Trusted_Connection=True;"
```

**Option B: SQL Server**
```json
"ConnectionString": "Server=localhost;Database=Compost;User Id=sa;Password=YourPassword;"
```

**Option C: PostgreSQL**
```json
"ConnectionString": "Host=localhost;Database=compost;Username=postgres;Password=yourpassword",
"DatabaseProvider": "Postgres"
```

#### 3. Run the Application
```bash
cd src/Compost.Web
dotnet run
```

Navigate to: `https://localhost:5001`

### First-Time Setup Wizard

1. **Site Settings**
   - Site Name: `Compost`
   - Recipe: Choose "Blank Site"
   - Time Zone: Your timezone

2. **Database**
   - Use the connection string from appsettings.json
   - Table Prefix: Leave blank or use `Compost_`

3. **Admin User**
   - Username: Choose your username
   - Email: Your email
   - Password: Secure password

4. **Click "Finish Setup"**

### Enable Compost Modules

After setup, go to:
1. **Admin Dashboard** → **Configuration** → **Features**
2. Enable these modules in order:
   - ✅ Compost Contexts
   - ✅ Compost Mind Map
   - ✅ Compost Transcription
   - ✅ Compost Kanban
   - ✅ Compost Snippets
   - ✅ Compost Patterns

3. Enable the **Compost Theme**:
   - Go to **Design** → **Themes**
   - Set "Compost Theme" as the current theme

### Create Content Types

Go to **Content Definition** → **Content Types** → **Create new type**

#### WorkContext Content Type
```
Name: WorkContext
Display Name: Work Context

Parts:
- Title Part
- Autoroute Part
- Work Context Part (custom)
- Flow Part (for flexible content)
- Markdown Body Part

Enable: Creatable, Listable, Draftable, Securable
```

#### MindMapNode Content Type
```
Name: MindMapNode  
Display Name: Mind Map Node

Parts:
- Title Part
- Markdown Body Part
- Mind Map Node Part (custom)
- Contained Part (contained in WorkContext)

Enable: Creatable, Listable
```

### Configure Azure Services

Edit `appsettings.json`:

```json
{
  "Compost": {
    "AzureSpeech": {
      "SubscriptionKey": "your-key-here",
      "Region": "eastus"
    },
    "BlobStorage": {
      "ConnectionString": "your-connection-string",
      "MeetingsContainer": "meetings",
      "SnippetsContainer": "snippets"
    },
    "OpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "Key": "your-key",
      "DeploymentName": "gpt-4"
    }
  }
}
```

## 📋 Module Overview

### Compost.Contexts
**Purpose:** Workspace and time tracking  
**Features:**
- Create work contexts for different projects
- Track time spent in each context
- Store repository info, testing steps, open questions
- Context switching with automatic time tracking

**Usage:**
1. Go to **Content** → **Content Items** → **Create** → **Work Context**
2. Fill in context details
3. Save and publish
4. Switch contexts from the dashboard

### Compost.MindMap
**Purpose:** Visual mind mapping with Cytoscape.js  
**Features:**
- Interactive drag-and-drop mind maps
- Different node types (Idea, Requirement, Action, etc.)
- Zoom, pan, node connections
- Promote nodes to tree structure
- Export as image

**Usage:**
1. Navigate to a Work Context
2. Click "Mind Map" tab
3. Double-click canvas to create nodes
4. Drag to position, right-click for context menu
5. Connect nodes by dragging from one to another

**Keyboard Shortcuts:**
- `Double Click`: Create node
- `Drag`: Move node
- `Right Click`: Context menu
- `Scroll`: Zoom
- `Click + Drag background`: Pan

### Compost.Transcription  
**Purpose:** Meeting recording and transcription  
**Features:**
- Browser-based audio recording (5-minute max)
- Real-time transcription with Azure Speech
- Speaker identification
- Auto-extract mind map nodes from transcript
- Link transcripts to contexts

**Usage:**
1. Go to a Work Context
2. Click "Record Meeting"
3. Allow microphone access
4. Speak (watch 5-minute timer)
5. Click "Stop" when done
6. Review transcript and extracted nodes

### Compost.Kanban
**Purpose:** Task board with story points  
**Features:**
- Drag-drop kanban board
- Story point estimation with AI suggestions
- Status columns: Backlog → Ready → In Progress → Review → Done
- Link to tree nodes and mind map nodes
- Time tracking per card

**Usage:**
1. Promote tree nodes to kanban cards
2. Or create cards directly
3. Drag cards between columns
4. Set story points (AI will suggest)
5. Track progress

### Compost.Snippets
**Purpose:** Searchable code repository  
**Features:**
- Store code snippets with syntax highlighting
- Search across all projects
- Tag and categorize
- Link to architectural patterns
- Most-used snippet tracking

**Usage:**
1. Go to **Content** → **Code Snippets** → **Create**
2. Paste code, select language
3. Add tags, category, project name
4. Search from anywhere using the search box

### Compost.Patterns
**Purpose:** Architectural pattern library  
**Features:**
- Template structure (When to use, How it works, Gotchas)
- Link to code snippets and projects
- AI-powered pattern suggestions
- Learn from your choices over time

**Usage:**
1. Create patterns from **Content** → **Architectural Patterns**
2. Link related code snippets
3. Add project references
4. Pattern suggestions appear when refining requirements

## 🎨 Theme Customization

### Compost Theme Structure
```
Compost.Theme/
├── wwwroot/
│   ├── css/
│   │   └── compost.css        # Main styles
│   ├── js/
│   │   └── compost.js         # UI enhancements
│   └── images/
├── Views/
│   ├── Layout.cshtml          # Main layout
│   └── [specific views]
└── Manifest.cs
```

### Customize Colors
Edit `wwwroot/css/compost.css`:
```css
:root {
    --color-primary: #2E7D32;
    --color-secondary: #558B2F;
    --color-accent: #81C784;
    /* ... */
}
```

## 🔧 Development Workflow

### Adding Features

#### 1. Create a new module
```bash
dotnet new orchardcore.module -n Compost.YourModule
```

#### 2. Add to solution
```bash
dotnet sln add src/Modules/Compost.YourModule
```

#### 3. Reference in Compost.Web
Edit `Compost.Web.csproj`:
```xml
<ProjectReference Include="..\Modules\Compost.YourModule\Compost.YourModule.csproj" />
```

#### 4. Create Manifest.cs
```csharp
[assembly: Module(
    Name = "Your Module",
    Author = "Compost Team",
    Version = "0.1.0",
    Description = "Description here"
)]
```

### Working with Content Parts

#### Create a Content Part
```csharp
public class YourCustomPart : ContentPart
{
    public string YourProperty { get; set; }
}
```

#### Create a Display Driver
```csharp
public class YourPartDisplayDriver : ContentPartDisplayDriver<YourCustomPart>
{
    public override IDisplayResult Display(YourCustomPart part)
    {
        return Initialize<YourViewModel>("YourPart", vm => {
            vm.Data = part.YourProperty;
        });
    }
}
```

#### Register in Startup.cs
```csharp
services.AddContentPart<YourCustomPart>();
services.AddScoped<IContentPartDisplayDriver, YourPartDisplayDriver>();
```

## 📱 Responsive Design

### Desktop (1024px+)
- Side-by-side panels
- Full mind map canvas
- Multi-column kanban board

### Tablet (768px - 1023px)
- Stacked layout with tabs
- Touch-optimized controls
- Collapsible sidebars

### Mobile (< 768px)
- Single column
- Hamburger menu
- Touch gestures for mind map

### Test on my Device

**Samsung Fold Z6:**
- Outer screen: Mobile layout
- Inner screen (unfolded): Tablet layout

**iPad:**
- Portrait: Tablet layout
- Landscape: Desktop layout

**Desktop:**
- Full desktop layout

## 🐛 Troubleshooting

### Database Connection Failed
```bash
# Check connection string in appsettings.json
# Ensure SQL Server is running
# Try: dotnet ef database update
```

### Module Not Showing
```bash
# Clear bin/obj folders
dotnet clean
dotnet build

# Check module is referenced in Compost.Web.csproj
# Enable module in Admin → Features
```

### Mind Map Not Loading
```bash
# Check browser console for errors
# Ensure Cytoscape.js is loaded (check network tab)
# Verify contextId is passed to JavaScript
```

### Recording Not Working
```bash
# Check microphone permissions in browser
# Ensure HTTPS (required for MediaRecorder API)
# Check Azure Speech Services credentials
```

## 🚀 Deployment

### Deploy to Azure App Service

#### 1. Publish the application
```bash
dotnet publish -c Release -o ./publish
```

#### 2. Create Azure resources
```bash
# App Service
az webapp create --name compost-app --resource-group compost-rg

# SQL Database
az sql db create --name compost-db --server compost-sql
```

#### 3. Configure connection string
In Azure Portal:
- App Service → Configuration → Connection Strings
- Add connection string with name: `OrchardCore_Shells_Database`

#### 4. Deploy
```bash
az webapp deployment source config-zip --src ./publish.zip
```

### Docker Deployment

Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Compost.Web.dll"]
```

Build and run:
```bash
docker build -t compost .
docker run -p 8080:80 compost
```

## 📊 Performance Tips

1. **Enable Output Caching**
```csharp
// In Startup.cs
services.AddOutputCache();
```

2. **Optimize Media**
- Use Azure CDN for static assets
- Enable media caching
- Compress images

3. **Database Indexing**
- Add indexes for frequently queried fields
- Monitor slow queries

4. **Browser Caching**
- Set cache headers for static assets
- Use service workers for offline

## 🎯 Next Steps

1. **Create your first Work Context**
2. **Record a test meeting**
3. **Build a mind map**
4. **Promote nodes to kanban**
5. **Add code snippets**
6. **Create architectural patterns**

## 📚 Resources

- [Orchard Core Documentation](https://docs.orchardcore.net/)
- [Cytoscape.js Documentation](https://js.cytoscape.org/)
- [Azure Speech Services](https://learn.microsoft.com/azure/ai-services/speech-service/)
- [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/)

## 💡 Tips

- **Use Workflows** to automate common tasks
- **Create Templates** for recurring contexts
- **Export Data** regularly as backup
- **Monitor Logs** in App_Data/logs/
- **Use GraphQL** for custom integrations
