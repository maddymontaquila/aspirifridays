# Entra ID Authentication Setup for Bingo Admin

## Overview
This admin website now requires authentication using Microsoft Entra ID (formerly Azure Active Directory) with the specified tenant ID: `72f988bf-86f1-41af-91ab-2d7cd011db47`.

## App Registration Setup

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

### 4. Configure API Permissions (Optional)
The app uses basic OpenID Connect which requires these permissions (usually granted by default):
- **Microsoft Graph** > **openid**
- **Microsoft Graph** > **profile**
- **Microsoft Graph** > **email**

## Application Configuration

### Update appsettings.json
Replace the placeholder values in `appsettings.json`:

```json
{
  "Authentication": {
    "Microsoft": {
      "ClientId": "YOUR_APP_REGISTRATION_CLIENT_ID",
      "ClientSecret": "YOUR_CLIENT_SECRET_VALUE"
    }
  }
}
```

### Environment Variables (Recommended for Production)
Instead of storing secrets in appsettings.json, use environment variables:

```bash
# Windows
set Authentication__Microsoft__ClientId=your-client-id
set Authentication__Microsoft__ClientSecret=your-client-secret

# Linux/Mac
export Authentication__Microsoft__ClientId=your-client-id
export Authentication__Microsoft__ClientSecret=your-client-secret
```

### Azure Key Vault (Production Recommended)
For production, store the client secret in Azure Key Vault:

1. Create an Azure Key Vault
2. Add the client secret as a secret
3. Configure the application to read from Key Vault
4. Grant the application's managed identity access to the Key Vault

## Features Implemented

### Authentication Features
- ✅ Microsoft Entra ID authentication
- ✅ Tenant-specific authentication (`72f988bf-86f1-41af-91ab-2d7cd011db47`)
- ✅ Automatic redirect to login for unauthenticated users
- ✅ User information display in header
- ✅ Sign out functionality
- ✅ Admin pages protected with `[Authorize]` attribute

### SignalR Hub Access
- ✅ SignalR hub (`/bingohub`) remains **anonymous** for client connections
- ✅ Admin pages require authentication
- ✅ Proper CORS configuration maintained

### Security Features
- ✅ HTTPS redirection
- ✅ Secure cookie settings
- ✅ Anti-forgery tokens
- ✅ Proper logout handling

## Testing the Implementation

### 1. Start the Application
```bash
cd src/BingoBoard.Admin
dotnet run
```

### 2. Test Authentication Flow
1. Navigate to `https://localhost:7001` (or your configured port)
2. You should be redirected to the login page
3. Click "Sign in with Microsoft"
4. Complete the Microsoft authentication
5. You should be redirected back to the admin dashboard

### 3. Test SignalR (Client Access)
The SignalR hub should still be accessible to your Vue.js frontend without authentication.

## Troubleshooting

### Common Issues

1. **Redirect URI Mismatch**
   - Ensure the redirect URI in Azure matches your application URL
   - Check both HTTP and HTTPS variants

2. **Client Secret Expired**
   - Generate a new client secret in Azure Portal
   - Update the configuration

3. **Tenant Not Found**
   - Verify the tenant ID is correct
   - Ensure the user has access to the specified tenant

4. **CORS Issues**
   - The CORS configuration should remain unchanged for SignalR
   - Admin pages don't need CORS as they're server-rendered

### Logs to Check
- Authentication failures will appear in the application logs
- Check browser developer tools for redirect issues
- SignalR connection issues will show in both client and server logs

## Next Steps

### Additional Security (Optional)
1. **Role-based Authorization**: Add specific roles for different admin levels
2. **Conditional Access**: Configure Entra ID conditional access policies
3. **Multi-factor Authentication**: Require MFA for admin access
4. **API Permissions**: Add specific permissions if accessing Microsoft Graph

### Monitoring
1. **Application Insights**: Monitor authentication events
2. **Audit Logs**: Track admin actions
3. **Security Alerts**: Set up alerts for failed authentication attempts

## Support
For issues with this authentication setup, check:
1. Application logs for detailed error messages
2. Azure AD sign-in logs in the Azure Portal
3. Browser developer tools for client-side issues
