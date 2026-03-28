import { createBuilder, ContainerLifetime } from './.modules/aspire.js';

const builder = await createBuilder();

builder.addAzureContainerAppEnvironment("env");

// Version info (C# used reflection; in TypeScript we use env vars / package.json)
const commitSha = process.env.COMMIT_SHA ?? "dev";
const aspireVersion = process.env.ASPIRE_VERSION ?? "unknown";
const dotnetVersion = process.env.DOTNET_VERSION ?? "unknown";

// Parameters
const password = await builder.addParameter("admin-password", { secret: true });

// Redis cache
const cache = await builder.addRedis("cache");

// PostgreSQL
const postgres = await builder.addPostgres("postgres")
    .withLifetime(ContainerLifetime.Persistent);

const db = await postgres.addDatabase("db");
await db.withPostgresMcp();

// Migration service
const migrations = await builder.addProject("migrations", "./BingoBoard.MigrationService", "BingoBoard.MigrationService")
    .withEnvironmentParameter("Authentication__AdminPassword", password)
    .withReference(db)
    .waitFor(db);

// Admin / backend project
// TODO: publishAsAzureContainerApp not available on ProjectResource.
// See: https://github.com/dotnet/aspire/issues/15142, https://github.com/dotnet/aspire/issues/15152
const admin = await builder.addProject("boardadmin", "./BingoBoard.Admin", "https");
await admin.withReference(cache);
await admin.withReference(db);
await admin.withServiceReference(migrations);
await admin.waitFor(cache);
await admin.waitForCompletion(migrations);
await admin.withEnvironment("COMMIT_SHA", commitSha);
await admin.withEnvironment("ASPIRE_VERSION", aspireVersion);
await admin.withExternalHttpEndpoints();

// Vue.js frontend (Vite)
const frontend = await builder.addViteApp("bingoboard-dev", "./bingo-board")
    .withEnvironment("VITE_COMMIT_SHA", commitSha)
    .withEnvironment("VITE_DOTNET_VERSION", dotnetVersion)
    .withEnvironment("VITE_ASPIRE_VERSION", aspireVersion)
    .withServiceReference(admin)
    .waitFor(admin);

// YARP reverse proxy
// TODO: YARP route configuration blocked by codegen bug — addCluster maps to wrong capability.
const yarp = await builder.addYarp("bingoboard");
await yarp.publishWithStaticFiles(frontend);
await yarp.waitFor(admin);
await yarp.withIconName("SerialPort");
await yarp.withExternalHttpEndpoints();
// TODO: publishAsAzureContainerApp blocked. See: https://github.com/dotnet/aspire/issues/15142
await yarp.withExplicitStart();

// MAUI section (conditional on launch profile)
const launchProfile = process.env.DOTNET_LAUNCH_PROFILE;
if (launchProfile && launchProfile.toLowerCase() === "maui") {
    const adminEndpoint = await admin.getEndpoint("https");

    const publicDevTunnel = await builder.addDevTunnel("devtunnel-public", { allowAnonymous: true });
    await publicDevTunnel.withTunnelReference(adminEndpoint);

    const mauiapp = await builder.addMauiProject(
        "mauiapp",
        "BingoBoard.MauiHybrid/BingoBoard.MauiHybrid.csproj"
    );

    // iOS simulator
    // NOTE: C# used .WithReference(admin, publicDevTunnel) which is [AspireExportIgnore] in polyglot.
    // Tunnel reference is set up separately above; MAUI devices reference admin directly.
    await mauiapp.addiOSSimulator("mauiapp-ios")
        .excludeFromManifest()
        .withOtlpDevTunnel()
        .withServiceReference(admin);

    // Android emulator
    const android = await mauiapp.addAndroidEmulator("mauiapp-android");
    await android.excludeFromManifest();
    await android.withParentRelationship(mauiapp);
    await android.withOtlpDevTunnel();
    await android.withServiceReference(admin);

    // Mac Catalyst desktop
    await mauiapp.addMacCatalystDevice("mauiapp-maccatalyst")
        .excludeFromManifest()
        .withServiceReference(admin);

    // Windows desktop
    await mauiapp.addWindowsDevice("mauiapp-windows")
        .excludeFromManifest()
        .withServiceReference(admin);
}

await builder.build().run();
