using Microsoft.EntityFrameworkCore;
using MESv2.Api.Models;

namespace MESv2.Api.Data;

public static class DbInitializer
{
    /// <summary>
    /// Seeds only system reference data that must exist in all environments
    /// (work center types, annotation types, etc.). Idempotent -- skips if data already exists.
    /// Called on SQL Server (Azure SQL) startup after migrations.
    /// </summary>
    public static void SeedReferenceData(MesDbContext context)
    {
        if (context.WorkCenterTypes.Any())
            return;

        var wctRollsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var wctLongSeamId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var wctInspectionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var wctFitupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var wctRoundSeamId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var wctNameplateId = Guid.Parse("f0f0f0f0-f0f0-f0f0-f0f0-f0f0f0f0f0f0");
        var wctHydroId = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
        var wctXrayId = Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2");
        var wctSpotXrayId = Guid.Parse("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f3");
        var wctMaterialQueueId = Guid.Parse("f4f4f4f4-f4f4-f4f4-f4f4-f4f4f4f4f4f4");

        context.WorkCenterTypes.AddRange(
            new WorkCenterType { Id = wctRollsId, Name = "Rolls" },
            new WorkCenterType { Id = wctLongSeamId, Name = "Long Seam" },
            new WorkCenterType { Id = wctInspectionId, Name = "Inspection" },
            new WorkCenterType { Id = wctFitupId, Name = "Fitup" },
            new WorkCenterType { Id = wctRoundSeamId, Name = "Round Seam" },
            new WorkCenterType { Id = wctNameplateId, Name = "Nameplate" },
            new WorkCenterType { Id = wctHydroId, Name = "Hydro" },
            new WorkCenterType { Id = wctXrayId, Name = "X-Ray" },
            new WorkCenterType { Id = wctSpotXrayId, Name = "Spot X-Ray" },
            new WorkCenterType { Id = wctMaterialQueueId, Name = "Material Queue" }
        );

        if (!context.AnnotationTypes.Any())
        {
            context.AnnotationTypes.AddRange(
                new AnnotationType { Id = Guid.Parse("a1000001-0000-0000-0000-000000000001"), Name = "Note", Abbreviation = "N", RequiresResolution = false, OperatorCanCreate = true, DisplayColor = "#cc00ff" },
                new AnnotationType { Id = Guid.Parse("a1000002-0000-0000-0000-000000000002"), Name = "AI Review", Abbreviation = "AI", RequiresResolution = false, OperatorCanCreate = false, DisplayColor = "#33cc33" },
                new AnnotationType { Id = Guid.Parse("a1000003-0000-0000-0000-000000000003"), Name = "Defect", Abbreviation = "D", RequiresResolution = true, OperatorCanCreate = true, DisplayColor = "#ff0000" },
                new AnnotationType { Id = Guid.Parse("a1000004-0000-0000-0000-000000000004"), Name = "Internal Review", Abbreviation = "IR", RequiresResolution = false, OperatorCanCreate = false, DisplayColor = "#0099ff" },
                new AnnotationType { Id = Guid.Parse("a1000005-0000-0000-0000-000000000005"), Name = "Correction Needed", Abbreviation = "C", RequiresResolution = true, OperatorCanCreate = true, DisplayColor = "#ffff00" }
            );
        }

        if (!context.ProductTypes.Any())
        {
            context.ProductTypes.AddRange(
                new ProductType { Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"), Name = "Plate", SystemTypeName = "plate" },
                new ProductType { Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"), Name = "Head", SystemTypeName = "head" },
                new ProductType { Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"), Name = "Shell", SystemTypeName = "shell" },
                new ProductType { Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"), Name = "Assembled Tank", SystemTypeName = "assembled" },
                new ProductType { Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"), Name = "Sellable Tank", SystemTypeName = "sellable" },
                new ProductType { Id = Guid.Parse("a6666666-6666-6666-6666-666666666666"), Name = "Plasma", SystemTypeName = "plasma" }
            );
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Full seed for local development (SQLite). Includes reference data plus test data.
    /// </summary>
    public static void Seed(MesDbContext context)
    {
        if (context.Plants.Any())
            return;

        var plant1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var plant2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var plant3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

        context.Plants.AddRange(
            new Plant { Id = plant1Id, Code = "000", Name = "Cleveland", TimeZoneId = "America/Chicago" },
            new Plant { Id = plant2Id, Code = "600", Name = "Fremont", TimeZoneId = "America/New_York" },
            new Plant { Id = plant3Id, Code = "700", Name = "West Jordan", TimeZoneId = "America/Denver" }
        );

        var wctRollsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var wctLongSeamId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var wctInspectionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var wctFitupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var wctRoundSeamId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var wctNameplateId = Guid.Parse("f0f0f0f0-f0f0-f0f0-f0f0-f0f0f0f0f0f0");
        var wctHydroId = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
        var wctXrayId = Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2");
        var wctSpotXrayId = Guid.Parse("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f3");
        var wctMaterialQueueId = Guid.Parse("f4f4f4f4-f4f4-f4f4-f4f4-f4f4f4f4f4f4");

        context.WorkCenterTypes.AddRange(
            new WorkCenterType { Id = wctRollsId, Name = "Rolls" },
            new WorkCenterType { Id = wctLongSeamId, Name = "Long Seam" },
            new WorkCenterType { Id = wctInspectionId, Name = "Inspection" },
            new WorkCenterType { Id = wctFitupId, Name = "Fitup" },
            new WorkCenterType { Id = wctRoundSeamId, Name = "Round Seam" },
            new WorkCenterType { Id = wctNameplateId, Name = "Nameplate" },
            new WorkCenterType { Id = wctHydroId, Name = "Hydro" },
            new WorkCenterType { Id = wctXrayId, Name = "X-Ray" },
            new WorkCenterType { Id = wctSpotXrayId, Name = "Spot X-Ray" },
            new WorkCenterType { Id = wctMaterialQueueId, Name = "Material Queue" }
        );

        var line1Plt1 = Guid.Parse("e1111111-1111-1111-1111-111111111111");
        var line2Plt1 = Guid.Parse("e2111111-1111-1111-1111-111111111111");
        var line1Plt2 = Guid.Parse("e1222222-2222-2222-2222-222222222222");
        var line2Plt2 = Guid.Parse("e2222222-2222-2222-2222-222222222222");
        var line1Plt3 = Guid.Parse("e1333333-3333-3333-3333-333333333333");

        context.ProductionLines.AddRange(
            new ProductionLine { Id = line1Plt1, Name = "Line 1", PlantId = plant1Id },
            new ProductionLine { Id = line2Plt1, Name = "Line 2", PlantId = plant1Id },
            new ProductionLine { Id = line1Plt2, Name = "Line 1", PlantId = plant2Id },
            new ProductionLine { Id = line2Plt2, Name = "Line 2", PlantId = plant2Id },
            new ProductionLine { Id = line1Plt3, Name = "Line 1", PlantId = plant3Id }
        );

        var wcRolls = Guid.Parse("f1111111-1111-1111-1111-111111111111");
        var wcLongSeam = Guid.Parse("f2111111-1111-1111-1111-111111111111");
        var wcLongSeamInsp = Guid.Parse("f3111111-1111-1111-1111-111111111111");
        var wcRtXrayQueue = Guid.Parse("f4111111-1111-1111-1111-111111111111");
        var wcFitup = Guid.Parse("f5111111-1111-1111-1111-111111111111");
        var wcRoundSeam = Guid.Parse("f6111111-1111-1111-1111-111111111111");
        var wcRoundSeamInsp = Guid.Parse("f7111111-1111-1111-1111-111111111111");
        var wcSpotXray = Guid.Parse("f8111111-1111-1111-1111-111111111111");
        var wcNameplate = Guid.Parse("f9111111-1111-1111-1111-111111111111");
        var wcHydro = Guid.Parse("fa111111-1111-1111-1111-111111111111");
        var wcRollsMaterial = Guid.Parse("fb111111-1111-1111-1111-111111111111");
        var wcFitupQueue = Guid.Parse("fc111111-1111-1111-1111-111111111111");

        context.WorkCenters.AddRange(
            new WorkCenter { Id = wcRolls, Name = "Rolls", WorkCenterTypeId = wctRollsId, NumberOfWelders = 1, DataEntryType = "Rolls" },
            new WorkCenter { Id = wcLongSeam, Name = "Long Seam", WorkCenterTypeId = wctLongSeamId, NumberOfWelders = 1, DataEntryType = "Barcode-LongSeam" },
            new WorkCenter { Id = wcLongSeamInsp, Name = "Long Seam Inspection", WorkCenterTypeId = wctInspectionId, NumberOfWelders = 0, DataEntryType = "Barcode-LongSeamInsp" },
            new WorkCenter { Id = wcRtXrayQueue, Name = "RT X-ray Queue", WorkCenterTypeId = wctXrayId, NumberOfWelders = 0, DataEntryType = "MatQueue-Shell" },
            new WorkCenter { Id = wcFitup, Name = "Fitup", WorkCenterTypeId = wctFitupId, NumberOfWelders = 1, DataEntryType = "Fitup" },
            new WorkCenter { Id = wcRoundSeam, Name = "Round Seam", WorkCenterTypeId = wctRoundSeamId, NumberOfWelders = 1, DataEntryType = "Barcode-RoundSeam" },
            new WorkCenter { Id = wcRoundSeamInsp, Name = "Round Seam Inspection", WorkCenterTypeId = wctInspectionId, NumberOfWelders = 0, DataEntryType = "Barcode-RoundSeamInsp" },
            new WorkCenter { Id = wcSpotXray, Name = "Spot X-ray", WorkCenterTypeId = wctSpotXrayId, NumberOfWelders = 0, DataEntryType = "Spot" },
            new WorkCenter { Id = wcNameplate, Name = "Nameplate", WorkCenterTypeId = wctNameplateId, NumberOfWelders = 0, DataEntryType = "DataPlate" },
            new WorkCenter { Id = wcHydro, Name = "Hydro", WorkCenterTypeId = wctHydroId, NumberOfWelders = 0, DataEntryType = "Hydro" },
            new WorkCenter { Id = wcRollsMaterial, Name = "Rolls Material", WorkCenterTypeId = wctMaterialQueueId, NumberOfWelders = 0, DataEntryType = "MatQueue-Material", MaterialQueueForWCId = wcRolls },
            new WorkCenter { Id = wcFitupQueue, Name = "Fitup Queue", WorkCenterTypeId = wctMaterialQueueId, NumberOfWelders = 0, DataEntryType = "MatQueue-Fitup", MaterialQueueForWCId = wcFitup }
        );

        var ptPlateId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
        var ptHeadId = Guid.Parse("a2222222-2222-2222-2222-222222222222");
        var ptShellId = Guid.Parse("a3333333-3333-3333-3333-333333333333");
        var ptAssembledTankId = Guid.Parse("a4444444-4444-4444-4444-444444444444");
        var ptSellableTankId = Guid.Parse("a5555555-5555-5555-5555-555555555555");
        var ptPlasmaId = Guid.Parse("a6666666-6666-6666-6666-666666666666");

        context.ProductTypes.AddRange(
            new ProductType { Id = ptPlateId, Name = "Plate", SystemTypeName = "plate" },
            new ProductType { Id = ptHeadId, Name = "Head", SystemTypeName = "head" },
            new ProductType { Id = ptShellId, Name = "Shell", SystemTypeName = "shell" },
            new ProductType { Id = ptAssembledTankId, Name = "Assembled Tank", SystemTypeName = "assembled" },
            new ProductType { Id = ptSellableTankId, Name = "Sellable Tank", SystemTypeName = "sellable" },
            new ProductType { Id = ptPlasmaId, Name = "Plasma", SystemTypeName = "plasma" }
        );

        var allSites = "000,600,700";
        var clevelandFremont = "000,600";
        context.Products.AddRange(
            new Product { Id = Guid.Parse("b1011111-1111-1111-1111-111111111111"), ProductNumber = "PL .140NOM X 54.00 X 74.625", TankSize = 120, TankType = "Plate", SiteNumbers = allSites, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b1021111-1111-1111-1111-111111111111"), ProductNumber = "PL .175NOM X 63.25 X 93.375", TankSize = 250, TankType = "Plate", SiteNumbers = allSites, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b1031111-1111-1111-1111-111111111111"), ProductNumber = "PL .175NOM X 87.00 X 93.375", TankSize = 320, TankType = "Plate", SiteNumbers = allSites, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b1041111-1111-1111-1111-111111111111"), ProductNumber = "PL .218NOM X 83.00 X 116.6875", TankSize = 500, TankType = "Plate", SiteNumbers = clevelandFremont, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b1051111-1111-1111-1111-111111111111"), ProductNumber = "PL .239NOM X 75.75 X 127.5675", TankSize = 1000, TankType = "Plate", SiteNumbers = clevelandFremont, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b2011111-1111-1111-1111-111111111111"), ProductNumber = "ELLIP 24\" OD", TankSize = 120, TankType = "Head", SiteNumbers = allSites, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b2021111-1111-1111-1111-111111111111"), ProductNumber = "HEMI 30\" OD", TankSize = 250, TankType = "Head", SiteNumbers = allSites, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b2031111-1111-1111-1111-111111111111"), ProductNumber = "HEMI 30\" OD", TankSize = 320, TankType = "Head", SiteNumbers = allSites, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b2041111-1111-1111-1111-111111111111"), ProductNumber = "HEMI 37\" ID", TankSize = 500, TankType = "Head", SiteNumbers = clevelandFremont, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b2051111-1111-1111-1111-111111111111"), ProductNumber = "HEMI 40.5\" ID", TankSize = 1000, TankType = "Head", SiteNumbers = clevelandFremont, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b3011111-1111-1111-1111-111111111111"), ProductNumber = "120 gal", TankSize = 120, TankType = "Shell", SiteNumbers = allSites, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b3021111-1111-1111-1111-111111111111"), ProductNumber = "250 gal", TankSize = 250, TankType = "Shell", SiteNumbers = allSites, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b3031111-1111-1111-1111-111111111111"), ProductNumber = "320 gal", TankSize = 320, TankType = "Shell", SiteNumbers = allSites, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b3041111-1111-1111-1111-111111111111"), ProductNumber = "500 gal", TankSize = 500, TankType = "Shell", SiteNumbers = clevelandFremont, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b3051111-1111-1111-1111-111111111111"), ProductNumber = "1000 gal", TankSize = 1000, TankType = "Shell", SiteNumbers = clevelandFremont, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b4011111-1111-1111-1111-111111111111"), ProductNumber = "120 gal Assembled", TankSize = 120, TankType = "Assembled", SiteNumbers = allSites, ProductTypeId = ptAssembledTankId },
            new Product { Id = Guid.Parse("b4021111-1111-1111-1111-111111111111"), ProductNumber = "250 gal Assembled", TankSize = 250, TankType = "Assembled", SiteNumbers = allSites, ProductTypeId = ptAssembledTankId },
            new Product { Id = Guid.Parse("b4031111-1111-1111-1111-111111111111"), ProductNumber = "320 gal Assembled", TankSize = 320, TankType = "Assembled", SiteNumbers = allSites, ProductTypeId = ptAssembledTankId },
            new Product { Id = Guid.Parse("b4041111-1111-1111-1111-111111111111"), ProductNumber = "500 gal Assembled", TankSize = 500, TankType = "Assembled", SiteNumbers = clevelandFremont, ProductTypeId = ptAssembledTankId },
            new Product { Id = Guid.Parse("b4051111-1111-1111-1111-111111111111"), ProductNumber = "1000 gal Assembled", TankSize = 1000, TankType = "Assembled", SiteNumbers = clevelandFremont, ProductTypeId = ptAssembledTankId },
            new Product { Id = Guid.Parse("b5011111-1111-1111-1111-111111111111"), ProductNumber = "120 AG", TankSize = 120, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5021111-1111-1111-1111-111111111111"), ProductNumber = "120 UG", TankSize = 120, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5031111-1111-1111-1111-111111111111"), ProductNumber = "250 AG", TankSize = 250, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5041111-1111-1111-1111-111111111111"), ProductNumber = "250 UG", TankSize = 250, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5051111-1111-1111-1111-111111111111"), ProductNumber = "320 AG", TankSize = 320, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5061111-1111-1111-1111-111111111111"), ProductNumber = "320 UG", TankSize = 320, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5071111-1111-1111-1111-111111111111"), ProductNumber = "500 AG", TankSize = 500, TankType = "Sellable", SiteNumbers = clevelandFremont, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5081111-1111-1111-1111-111111111111"), ProductNumber = "500 UG", TankSize = 500, TankType = "Sellable", SiteNumbers = clevelandFremont, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5091111-1111-1111-1111-111111111111"), ProductNumber = "1000 AG", TankSize = 1000, TankType = "Sellable", SiteNumbers = clevelandFremont, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b50a1111-1111-1111-1111-111111111111"), ProductNumber = "1000 UG", TankSize = 1000, TankType = "Sellable", SiteNumbers = clevelandFremont, ProductTypeId = ptSellableTankId }
        );

        context.Users.AddRange(
            new User { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), EmployeeNumber = "EMP001", FirstName = "Jeff", LastName = "Thompson", DisplayName = "Jeff Thompson", RoleTier = 1.0m, RoleName = "Administrator", DefaultSiteId = plant1Id, IsCertifiedWelder = false, RequirePinForLogin = false, PinHash = null },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888801"), EmployeeNumber = "EMP002", FirstName = "Sarah", LastName = "Miller", DisplayName = "Sarah Miller", RoleTier = 6.0m, RoleName = "Operator", DefaultSiteId = plant1Id, IsCertifiedWelder = false, RequirePinForLogin = false, PinHash = null },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888802"), EmployeeNumber = "EMP003", FirstName = "Mike", LastName = "Rodriguez", DisplayName = "Mike Rodriguez", RoleTier = 6.0m, RoleName = "Operator", DefaultSiteId = plant1Id, IsCertifiedWelder = true, RequirePinForLogin = false, PinHash = null },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888803"), EmployeeNumber = "EMP004", FirstName = "Tom", LastName = "Wilson", DisplayName = "Tom Wilson", RoleTier = 6.0m, RoleName = "Operator", DefaultSiteId = plant2Id, IsCertifiedWelder = true, RequirePinForLogin = false, PinHash = null },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888804"), EmployeeNumber = "EMP005", FirstName = "Lisa", LastName = "Chen", DisplayName = "Lisa Chen", RoleTier = 4.0m, RoleName = "Supervisor", DefaultSiteId = plant1Id, IsCertifiedWelder = false, RequirePinForLogin = true, PinHash = BCrypt.Net.BCrypt.HashPassword("1234") },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888805"), EmployeeNumber = "AI99001", FirstName = "Bob", LastName = "Harrison", DisplayName = "Bob Harrison", RoleTier = 5.5m, RoleName = "Authorized Inspector", DefaultSiteId = plant1Id, IsCertifiedWelder = false, RequirePinForLogin = false, PinHash = null, UserType = UserType.AuthorizedInspector }
        );

        context.Vendors.AddRange(
            new Vendor { Id = Guid.Parse("51000001-0000-0000-0000-000000000001"), Name = "Nucor Steel", VendorType = "mill", PlantIds = $"{plant1Id},{plant2Id},{plant3Id}", IsActive = true },
            new Vendor { Id = Guid.Parse("51000002-0000-0000-0000-000000000002"), Name = "Steel Dynamics", VendorType = "mill", PlantIds = $"{plant1Id},{plant2Id}", IsActive = true },
            new Vendor { Id = Guid.Parse("51000003-0000-0000-0000-000000000003"), Name = "NLMK", VendorType = "mill", PlantIds = $"{plant3Id}", IsActive = true },
            new Vendor { Id = Guid.Parse("52000001-0000-0000-0000-000000000001"), Name = "Metals USA", VendorType = "processor", PlantIds = $"{plant1Id},{plant2Id},{plant3Id}", IsActive = true },
            new Vendor { Id = Guid.Parse("52000002-0000-0000-0000-000000000002"), Name = "Steel Technologies", VendorType = "processor", PlantIds = $"{plant1Id}", IsActive = true },
            new Vendor { Id = Guid.Parse("53000001-0000-0000-0000-000000000001"), Name = "CMF Inc", VendorType = "head", PlantIds = $"{plant1Id},{plant2Id}", IsActive = true },
            new Vendor { Id = Guid.Parse("53000002-0000-0000-0000-000000000002"), Name = "Compco Industries", VendorType = "head", PlantIds = $"{plant1Id},{plant2Id},{plant3Id}", IsActive = true }
        );

        var plantCodeMap = new Dictionary<string, Guid>
        {
            ["000"] = plant1Id,
            ["600"] = plant2Id,
            ["700"] = plant3Id
        };
        var ppList = new List<ProductPlant>();
        foreach (var product in context.ChangeTracker.Entries<Product>()
            .Where(e => e.State == EntityState.Added && !string.IsNullOrEmpty(e.Entity.SiteNumbers))
            .Select(e => e.Entity))
        {
            foreach (var code in product.SiteNumbers!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (plantCodeMap.TryGetValue(code, out var pid))
                    ppList.Add(new ProductPlant { Id = Guid.NewGuid(), ProductId = product.Id, PlantId = pid });
            }
        }
        context.ProductPlants.AddRange(ppList);

        var vpList = new List<VendorPlant>();
        foreach (var vendor in context.ChangeTracker.Entries<Vendor>()
            .Where(e => e.State == EntityState.Added && !string.IsNullOrEmpty(e.Entity.PlantIds))
            .Select(e => e.Entity))
        {
            foreach (var seg in vendor.PlantIds!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(seg, out var pid))
                    vpList.Add(new VendorPlant { Id = Guid.NewGuid(), VendorId = vendor.Id, PlantId = pid });
            }
        }
        context.VendorPlants.AddRange(vpList);

        context.PlantGears.AddRange(
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111111"), Name = "Gear 1", Level = 1, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111112"), Name = "Gear 2", Level = 2, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111113"), Name = "Gear 3", Level = 3, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111114"), Name = "Gear 4", Level = 4, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111115"), Name = "Gear 5", Level = 5, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222221"), Name = "Gear 1", Level = 1, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222222"), Name = "Gear 2", Level = 2, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222223"), Name = "Gear 3", Level = 3, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222224"), Name = "Gear 4", Level = 4, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222225"), Name = "Gear 5", Level = 5, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333331"), Name = "Gear 1", Level = 1, PlantId = plant3Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333332"), Name = "Gear 2", Level = 2, PlantId = plant3Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333333"), Name = "Gear 3", Level = 3, PlantId = plant3Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333334"), Name = "Gear 4", Level = 4, PlantId = plant3Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333335"), Name = "Gear 5", Level = 5, PlantId = plant3Id }
        );

        context.AnnotationTypes.AddRange(
            new AnnotationType { Id = Guid.Parse("a1000001-0000-0000-0000-000000000001"), Name = "Note", Abbreviation = "N", RequiresResolution = false, OperatorCanCreate = true, DisplayColor = "#cc00ff" },
            new AnnotationType { Id = Guid.Parse("a1000002-0000-0000-0000-000000000002"), Name = "AI Review", Abbreviation = "AI", RequiresResolution = false, OperatorCanCreate = false, DisplayColor = "#33cc33" },
            new AnnotationType { Id = Guid.Parse("a1000003-0000-0000-0000-000000000003"), Name = "Defect", Abbreviation = "D", RequiresResolution = true, OperatorCanCreate = true, DisplayColor = "#ff0000" },
            new AnnotationType { Id = Guid.Parse("a1000004-0000-0000-0000-000000000004"), Name = "Internal Review", Abbreviation = "IR", RequiresResolution = false, OperatorCanCreate = false, DisplayColor = "#0099ff" },
            new AnnotationType { Id = Guid.Parse("a1000005-0000-0000-0000-000000000005"), Name = "Correction Needed", Abbreviation = "C", RequiresResolution = true, OperatorCanCreate = true, DisplayColor = "#ffff00" }
        );

        var charLongSeamId = Guid.Parse("c1000001-0000-0000-0000-000000000001");
        var charRs1Id = Guid.Parse("c2000001-0000-0000-0000-000000000001");
        var charRs2Id = Guid.Parse("c2000002-0000-0000-0000-000000000002");
        var charRs3Id = Guid.Parse("c2000003-0000-0000-0000-000000000003");
        var charRs4Id = Guid.Parse("c2000004-0000-0000-0000-000000000004");

        context.Characteristics.AddRange(
            new Characteristic { Id = charLongSeamId, Name = "Long Seam", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null },
            new Characteristic { Id = charRs1Id, Name = "RS1", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null },
            new Characteristic { Id = charRs2Id, Name = "RS2", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null },
            new Characteristic { Id = charRs3Id, Name = "RS3", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null },
            new Characteristic { Id = charRs4Id, Name = "RS4", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null }
        );

        context.CharacteristicWorkCenters.AddRange(
            new CharacteristicWorkCenter { Id = Guid.Parse("c1000001-0000-0000-0000-000000000001"), CharacteristicId = charLongSeamId, WorkCenterId = wcLongSeamInsp },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000001-0000-0000-0000-000000000001"), CharacteristicId = charRs1Id, WorkCenterId = wcRoundSeamInsp },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000002-0000-0000-0000-000000000001"), CharacteristicId = charRs2Id, WorkCenterId = wcRoundSeamInsp },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000003-0000-0000-0000-000000000001"), CharacteristicId = charRs3Id, WorkCenterId = wcRoundSeamInsp },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000004-0000-0000-0000-000000000001"), CharacteristicId = charRs4Id, WorkCenterId = wcRoundSeamInsp }
        );

        var dc101Id = Guid.Parse("d1010001-0000-0000-0000-000000000001");
        var dc102Id = Guid.Parse("d1010002-0000-0000-0000-000000000002");
        var dc103Id = Guid.Parse("d1010003-0000-0000-0000-000000000003");
        var dc104Id = Guid.Parse("d1010004-0000-0000-0000-000000000004");
        var dc105Id = Guid.Parse("d1010005-0000-0000-0000-000000000005");
        var dc999Id = Guid.Parse("d9990001-0000-0000-0000-000000000001");

        context.DefectCodes.AddRange(
            new DefectCode { Id = dc101Id, Code = "101", Name = "Burn Through", Severity = null, SystemType = null },
            new DefectCode { Id = dc102Id, Code = "102", Name = "Undercut", Severity = null, SystemType = null },
            new DefectCode { Id = dc103Id, Code = "103", Name = "Cold Lap", Severity = null, SystemType = null },
            new DefectCode { Id = dc104Id, Code = "104", Name = "Porosity", Severity = null, SystemType = null },
            new DefectCode { Id = dc105Id, Code = "105", Name = "Crack", Severity = null, SystemType = null },
            new DefectCode { Id = dc999Id, Code = "999", Name = "Shell Plate Thickness", Severity = null, SystemType = null }
        );

        context.DefectWorkCenters.AddRange(
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcLongSeamInsp, EarliestDetectionWorkCenterId = wcLongSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcLongSeamInsp, EarliestDetectionWorkCenterId = wcLongSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcLongSeamInsp, EarliestDetectionWorkCenterId = wcLongSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcLongSeamInsp, EarliestDetectionWorkCenterId = wcLongSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcLongSeamInsp, EarliestDetectionWorkCenterId = wcLongSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcLongSeamInsp, EarliestDetectionWorkCenterId = wcLongSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcRoundSeamInsp, EarliestDetectionWorkCenterId = wcRoundSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcRoundSeamInsp, EarliestDetectionWorkCenterId = wcRoundSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcRoundSeamInsp, EarliestDetectionWorkCenterId = wcRoundSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcRoundSeamInsp, EarliestDetectionWorkCenterId = wcRoundSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcRoundSeamInsp, EarliestDetectionWorkCenterId = wcRoundSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcRoundSeamInsp, EarliestDetectionWorkCenterId = wcRoundSeamInsp },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcHydro, EarliestDetectionWorkCenterId = wcHydro },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcHydro, EarliestDetectionWorkCenterId = wcHydro },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcHydro, EarliestDetectionWorkCenterId = wcHydro },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcHydro, EarliestDetectionWorkCenterId = wcHydro },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcHydro, EarliestDetectionWorkCenterId = wcHydro },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcHydro, EarliestDetectionWorkCenterId = wcHydro }
        );

        context.DefectLocations.AddRange(
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000001"), Code = "1", Name = "T-Joint", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000002"), Code = "2", Name = "Tack", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000003"), Code = "3", Name = "Fill Valve", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000004"), Code = "4", Name = "Leg", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000005"), Code = "5", Name = "Start", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000006"), Code = "6", Name = "End", DefaultLocationDetail = null, CharacteristicId = charLongSeamId }
        );

        context.BarcodeCards.AddRange(
            new BarcodeCard { Id = Guid.Parse("bc000001-0000-0000-0000-000000000001"), CardValue = "01", Color = "Red", Description = null },
            new BarcodeCard { Id = Guid.Parse("bc000002-0000-0000-0000-000000000002"), CardValue = "02", Color = "Yellow", Description = null },
            new BarcodeCard { Id = Guid.Parse("bc000003-0000-0000-0000-000000000003"), CardValue = "03", Color = "Blue", Description = null },
            new BarcodeCard { Id = Guid.Parse("bc000004-0000-0000-0000-000000000004"), CardValue = "04", Color = "Green", Description = null },
            new BarcodeCard { Id = Guid.Parse("bc000005-0000-0000-0000-000000000005"), CardValue = "05", Color = "Orange", Description = null }
        );

        context.Assets.AddRange(
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000001"), Name = "Rolls Asset", WorkCenterId = wcRolls, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000002"), Name = "Long Seam Asset", WorkCenterId = wcLongSeam, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000003"), Name = "Fitup Asset", WorkCenterId = wcFitup, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000004"), Name = "Round Seam Asset", WorkCenterId = wcRoundSeam, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000005"), Name = "Hydro Asset", WorkCenterId = wcHydro, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000002-0000-0000-0000-000000000001"), Name = "Rolls Asset", WorkCenterId = wcRolls, ProductionLineId = line1Plt2 },
            new Asset { Id = Guid.Parse("a0000002-0000-0000-0000-000000000002"), Name = "Long Seam Asset", WorkCenterId = wcLongSeam, ProductionLineId = line1Plt2 },
            new Asset { Id = Guid.Parse("a0000002-0000-0000-0000-000000000003"), Name = "Fitup Asset", WorkCenterId = wcFitup, ProductionLineId = line1Plt2 },
            new Asset { Id = Guid.Parse("a0000002-0000-0000-0000-000000000004"), Name = "Round Seam Asset", WorkCenterId = wcRoundSeam, ProductionLineId = line1Plt2 },
            new Asset { Id = Guid.Parse("a0000002-0000-0000-0000-000000000005"), Name = "Hydro Asset", WorkCenterId = wcHydro, ProductionLineId = line1Plt2 },
            new Asset { Id = Guid.Parse("a0000003-0000-0000-0000-000000000001"), Name = "Rolls Asset", WorkCenterId = wcRolls, ProductionLineId = line1Plt3 },
            new Asset { Id = Guid.Parse("a0000003-0000-0000-0000-000000000002"), Name = "Long Seam Asset", WorkCenterId = wcLongSeam, ProductionLineId = line1Plt3 },
            new Asset { Id = Guid.Parse("a0000003-0000-0000-0000-000000000003"), Name = "Fitup Asset", WorkCenterId = wcFitup, ProductionLineId = line1Plt3 },
            new Asset { Id = Guid.Parse("a0000003-0000-0000-0000-000000000004"), Name = "Round Seam Asset", WorkCenterId = wcRoundSeam, ProductionLineId = line1Plt3 },
            new Asset { Id = Guid.Parse("a0000003-0000-0000-0000-000000000005"), Name = "Hydro Asset", WorkCenterId = wcHydro, ProductionLineId = line1Plt3 }
        );

        context.WorkCenterProductionLines.AddRange(
            // Plant 1, Line 1
            new WorkCenterProductionLine { Id = Guid.Parse("d0010001-0000-0000-0000-000000000001"), WorkCenterId = wcRolls, ProductionLineId = line1Plt1, DisplayName = "Rolls", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0010001-0000-0000-0000-000000000002"), WorkCenterId = wcLongSeam, ProductionLineId = line1Plt1, DisplayName = "Long Seam", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0010001-0000-0000-0000-000000000003"), WorkCenterId = wcLongSeamInsp, ProductionLineId = line1Plt1, DisplayName = "Long Seam Inspection", NumberOfWelders = 0 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0010001-0000-0000-0000-000000000004"), WorkCenterId = wcFitup, ProductionLineId = line1Plt1, DisplayName = "Fitup", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0010001-0000-0000-0000-000000000005"), WorkCenterId = wcRoundSeam, ProductionLineId = line1Plt1, DisplayName = "Round Seam", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0010001-0000-0000-0000-000000000006"), WorkCenterId = wcRoundSeamInsp, ProductionLineId = line1Plt1, DisplayName = "Round Seam Inspection", NumberOfWelders = 0 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0010001-0000-0000-0000-000000000007"), WorkCenterId = wcHydro, ProductionLineId = line1Plt1, DisplayName = "Hydro", NumberOfWelders = 0 },
            // Plant 2, Line 1
            new WorkCenterProductionLine { Id = Guid.Parse("d0020001-0000-0000-0000-000000000001"), WorkCenterId = wcRolls, ProductionLineId = line1Plt2, DisplayName = "Rolls", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0020001-0000-0000-0000-000000000002"), WorkCenterId = wcLongSeam, ProductionLineId = line1Plt2, DisplayName = "Long Seam", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0020001-0000-0000-0000-000000000003"), WorkCenterId = wcFitup, ProductionLineId = line1Plt2, DisplayName = "Fitup", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0020001-0000-0000-0000-000000000004"), WorkCenterId = wcRoundSeam, ProductionLineId = line1Plt2, DisplayName = "Round Seam", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0020001-0000-0000-0000-000000000005"), WorkCenterId = wcHydro, ProductionLineId = line1Plt2, DisplayName = "Hydro", NumberOfWelders = 0 },
            // Plant 3, Line 1
            new WorkCenterProductionLine { Id = Guid.Parse("d0030001-0000-0000-0000-000000000001"), WorkCenterId = wcRolls, ProductionLineId = line1Plt3, DisplayName = "Rolls", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0030001-0000-0000-0000-000000000002"), WorkCenterId = wcLongSeam, ProductionLineId = line1Plt3, DisplayName = "Long Seam", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0030001-0000-0000-0000-000000000003"), WorkCenterId = wcFitup, ProductionLineId = line1Plt3, DisplayName = "Fitup", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0030001-0000-0000-0000-000000000004"), WorkCenterId = wcRoundSeam, ProductionLineId = line1Plt3, DisplayName = "Round Seam", NumberOfWelders = 1 },
            new WorkCenterProductionLine { Id = Guid.Parse("d0030001-0000-0000-0000-000000000005"), WorkCenterId = wcHydro, ProductionLineId = line1Plt3, DisplayName = "Hydro", NumberOfWelders = 0 }
        );

        context.SaveChanges();
    }

    /// <summary>
    /// One-time sync: populates ProductPlant/VendorPlant rows for any Products or Vendors
    /// that have string-based site assignments but no corresponding join-table rows.
    /// Safe to call on every startup; no-ops when tables are already in sync.
    /// </summary>
    public static void SyncJoinTables(MesDbContext context)
    {
        var plantsByCode = context.Plants.ToDictionary(p => p.Code, p => p.Id);

        var productsToSync = context.Products
            .Where(p => p.SiteNumbers != null && p.SiteNumbers != ""
                && !context.ProductPlants.Any(pp => pp.ProductId == p.Id))
            .ToList();

        foreach (var product in productsToSync)
        {
            foreach (var code in product.SiteNumbers!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (plantsByCode.TryGetValue(code, out var plantId))
                    context.ProductPlants.Add(new ProductPlant { Id = Guid.NewGuid(), ProductId = product.Id, PlantId = plantId });
            }
        }

        var vendorsToSync = context.Vendors
            .Where(v => v.PlantIds != null && v.PlantIds != ""
                && !context.VendorPlants.Any(vp => vp.VendorId == v.Id))
            .ToList();

        foreach (var vendor in vendorsToSync)
        {
            foreach (var seg in vendor.PlantIds!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(seg, out var plantId))
                    context.VendorPlants.Add(new VendorPlant { Id = Guid.NewGuid(), VendorId = vendor.Id, PlantId = plantId });
            }
        }

        if (context.ChangeTracker.HasChanges())
            context.SaveChanges();
    }
}
