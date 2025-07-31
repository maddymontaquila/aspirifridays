const string AppRegistrationSetupInstructions = """
## Azure App Registration Setup for AspiriFridays Bingo Admin

### 1. Create App Registration in Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Fill in the details:
   - **Name**: `AspiriFridays Bingo Admin`
   - **Supported account types**: `Accounts in this organizational directory only`
   - **Redirect URI**: 
     - Type: `Web`
     - URL: `https://localhost:7001/signin-oidc` (adjust port as needed)

### 2. Configure Authentication

1. In your app registration, go to **Authentication**
2. Add these redirect URIs if not already present:
   - `https://localhost:7001/signin-oidc`
   - `http://localhost:5000/signin-oidc` (if using HTTP)
3. Under **Front-channel logout URL**, add:
   - `https://localhost:7001/signout-oidc`
4. Under **Implicit grant and hybrid flows**, enable:
   - ✅ **ID tokens**
5. Click **Save**

### 3. Create Client Secret

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Add description: `AspiriFridays Bingo Admin Secret`
4. Set expiration as appropriate
5. Click **Add**
6. **COPY THE SECRET VALUE** immediately (you won't be able to see it again)

### 4. Update Aspire Parameters

After creating the app registration, update these parameters:
- Set `client-id` parameter to your App Registration Client ID
- Set `client-secret` parameter to your Client Secret Value
- Verify `tenant-id` parameter matches your Azure tenant ID

### 5. Test Authentication

1. Run `aspire run`
2. Navigate to the admin site
3. You should be redirected to Microsoft login
4. After authentication, you'll be redirected back to the admin dashboard

### 6. Production Deployment

For production:
- Store client secret in Azure Key Vault
- Use managed identity where possible
- Configure proper redirect URIs for your production domains
- Enable proper logging and monitoring
""";

var builder = DistributedApplication.CreateBuilder(args);

// Add a development mode parameter to disable authentication
var developmentMode = builder.AddParameter("development-mode")
    .WithDescription("Set to 'true' to disable authentication for development (default: true in Development environment)");

// // Parameters for Azure authentication (only required when not in development mode)
// var tenantId = builder.AddParameter("tenant-id")
//     .WithDescription("The Azure AD tenant ID for authentication (e.g., 72f988bf-86f1-41af-91ab-2d7cd011db47)");

// var clientId = builder.AddParameter("client-id")
//     .WithDescription("The Azure AD app registration client ID");

// var clientSecret = builder.AddParameter("client-secret", secret: true)
//     .WithDescription("The Azure AD app registration client secret");

// // App registration setup instructions parameter with markdown
// var appRegistrationSetup = builder.AddParameter("app-registration-setup")
//     .WithDescription(AppRegistrationSetupInstructions, enableMarkdown: true);

var cache = builder.AddRedis("cache");

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WithEnvironment("Development__DisableAuthentication", developmentMode)
    // .WithEnvironment("Authentication__Microsoft__TenantId", tenantId)
    // .WithEnvironment("Authentication__Microsoft__ClientId", clientId)
    // .WithEnvironment("Authentication__Microsoft__ClientSecret", clientSecret)
    .WaitFor(cache);

var bingo = builder.AddViteApp("bingoboard", "../bingo-board")
    .WithNpmPackageInstallation()
    .WithEnvironment("PORT", "5173")
    .WithReference(admin)
    .WithEnvironment("VITE_ADMIN_URL", admin.GetEndpoint("https"))
    .WaitFor(admin);

admin.WithReference(bingo);

builder.Build().Run();
