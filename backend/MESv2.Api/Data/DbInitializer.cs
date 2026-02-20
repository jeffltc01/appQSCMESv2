using MESv2.Api.Models;

namespace MESv2.Api.Data;

public static class DbInitializer
{
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

        var grpRolls1 = Guid.Parse("d0000001-0000-0000-0000-000000000001");
        var grpLongSeam1 = Guid.Parse("d0000002-0000-0000-0000-000000000002");
        var grpLongSeamInsp = Guid.Parse("d0000003-0000-0000-0000-000000000003");
        var grpRtXrayQueue = Guid.Parse("d0000004-0000-0000-0000-000000000004");
        var grpFitup = Guid.Parse("d0000005-0000-0000-0000-000000000005");
        var grpRoundSeam = Guid.Parse("d0000006-0000-0000-0000-000000000006");
        var grpRoundSeamInsp = Guid.Parse("d0000007-0000-0000-0000-000000000007");
        var grpSpotXray = Guid.Parse("d0000008-0000-0000-0000-000000000008");
        var grpNameplate = Guid.Parse("d0000009-0000-0000-0000-000000000009");
        var grpHydro = Guid.Parse("d000000a-0000-0000-0000-00000000000a");
        var grpRollsMaterial = Guid.Parse("d000000b-0000-0000-0000-00000000000b");
        var grpFitupQueue = Guid.Parse("d000000c-0000-0000-0000-00000000000c");

        var wcRolls1Plt1 = Guid.Parse("f1111111-1111-1111-1111-111111111111");
        var wcLongSeam1Plt1 = Guid.Parse("f2111111-1111-1111-1111-111111111111");
        var wcLongSeamInspPlt1 = Guid.Parse("f3111111-1111-1111-1111-111111111111");
        var wcRtXrayQueuePlt1 = Guid.Parse("f4111111-1111-1111-1111-111111111111");
        var wcFitupPlt1 = Guid.Parse("f5111111-1111-1111-1111-111111111111");
        var wcRoundSeamPlt1 = Guid.Parse("f6111111-1111-1111-1111-111111111111");
        var wcRoundSeamInspPlt1 = Guid.Parse("f7111111-1111-1111-1111-111111111111");
        var wcSpotXrayPlt1 = Guid.Parse("f8111111-1111-1111-1111-111111111111");
        var wcNameplatePlt1 = Guid.Parse("f9111111-1111-1111-1111-111111111111");
        var wcHydroPlt1 = Guid.Parse("fa111111-1111-1111-1111-111111111111");
        var wcRollsMaterialPlt1 = Guid.Parse("fb111111-1111-1111-1111-111111111111");
        var wcFitupQueuePlt1 = Guid.Parse("fc111111-1111-1111-1111-111111111111");

        context.WorkCenters.AddRange(
            new WorkCenter { Id = wcRolls1Plt1, Name = "Rolls 1", PlantId = plant1Id, WorkCenterTypeId = wctRollsId, ProductionLineId = line1Plt1, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpRolls1 },
            new WorkCenter { Id = wcLongSeam1Plt1, Name = "Long Seam 1", PlantId = plant1Id, WorkCenterTypeId = wctLongSeamId, ProductionLineId = line1Plt1, NumberOfWelders = 1, DataEntryType = "standard", WorkCenterGroupId = grpLongSeam1 },
            new WorkCenter { Id = wcLongSeamInspPlt1, Name = "Long Seam Inspection", PlantId = plant1Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = "inspection", WorkCenterGroupId = grpLongSeamInsp },
            new WorkCenter { Id = wcRtXrayQueuePlt1, Name = "RT X-ray Queue", PlantId = plant1Id, WorkCenterTypeId = wctXrayId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpRtXrayQueue },
            new WorkCenter { Id = wcFitupPlt1, Name = "Fitup", PlantId = plant1Id, WorkCenterTypeId = wctFitupId, ProductionLineId = line1Plt1, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpFitup },
            new WorkCenter { Id = wcRoundSeamPlt1, Name = "Round Seam", PlantId = plant1Id, WorkCenterTypeId = wctRoundSeamId, ProductionLineId = line1Plt1, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpRoundSeam },
            new WorkCenter { Id = wcRoundSeamInspPlt1, Name = "Round Seam Inspection", PlantId = plant1Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = "inspection", WorkCenterGroupId = grpRoundSeamInsp },
            new WorkCenter { Id = wcSpotXrayPlt1, Name = "Spot X-ray", PlantId = plant1Id, WorkCenterTypeId = wctSpotXrayId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpSpotXray },
            new WorkCenter { Id = wcNameplatePlt1, Name = "Nameplate", PlantId = plant1Id, WorkCenterTypeId = wctNameplateId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpNameplate },
            new WorkCenter { Id = wcHydroPlt1, Name = "Hydro", PlantId = plant1Id, WorkCenterTypeId = wctHydroId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpHydro },
            new WorkCenter { Id = wcRollsMaterialPlt1, Name = "Rolls Material", PlantId = plant1Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = wcRolls1Plt1, WorkCenterGroupId = grpRollsMaterial },
            new WorkCenter { Id = wcFitupQueuePlt1, Name = "Fitup Queue", PlantId = plant1Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = wcFitupPlt1, WorkCenterGroupId = grpFitupQueue },
            new WorkCenter { Id = Guid.Parse("f1222222-2222-2222-2222-222222222222"), Name = "Rolls 1", PlantId = plant2Id, WorkCenterTypeId = wctRollsId, ProductionLineId = line1Plt2, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpRolls1 },
            new WorkCenter { Id = Guid.Parse("f2222222-2222-2222-2222-222222222222"), Name = "Long Seam 1", PlantId = plant2Id, WorkCenterTypeId = wctLongSeamId, ProductionLineId = line1Plt2, NumberOfWelders = 1, DataEntryType = "standard", WorkCenterGroupId = grpLongSeam1 },
            new WorkCenter { Id = Guid.Parse("f3222222-2222-2222-2222-222222222222"), Name = "Long Seam Inspection", PlantId = plant2Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = "inspection", WorkCenterGroupId = grpLongSeamInsp },
            new WorkCenter { Id = Guid.Parse("f4222222-2222-2222-2222-222222222222"), Name = "RT X-ray Queue", PlantId = plant2Id, WorkCenterTypeId = wctXrayId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpRtXrayQueue },
            new WorkCenter { Id = Guid.Parse("f5222222-2222-2222-2222-222222222222"), Name = "Fitup", PlantId = plant2Id, WorkCenterTypeId = wctFitupId, ProductionLineId = line1Plt2, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpFitup },
            new WorkCenter { Id = Guid.Parse("f6222222-2222-2222-2222-222222222222"), Name = "Round Seam", PlantId = plant2Id, WorkCenterTypeId = wctRoundSeamId, ProductionLineId = line1Plt2, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpRoundSeam },
            new WorkCenter { Id = Guid.Parse("f7222222-2222-2222-2222-222222222222"), Name = "Round Seam Inspection", PlantId = plant2Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = "inspection", WorkCenterGroupId = grpRoundSeamInsp },
            new WorkCenter { Id = Guid.Parse("f8222222-2222-2222-2222-222222222222"), Name = "Spot X-ray", PlantId = plant2Id, WorkCenterTypeId = wctSpotXrayId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpSpotXray },
            new WorkCenter { Id = Guid.Parse("f9222222-2222-2222-2222-222222222222"), Name = "Nameplate", PlantId = plant2Id, WorkCenterTypeId = wctNameplateId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpNameplate },
            new WorkCenter { Id = Guid.Parse("fa222222-2222-2222-2222-222222222222"), Name = "Hydro", PlantId = plant2Id, WorkCenterTypeId = wctHydroId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpHydro },
            new WorkCenter { Id = Guid.Parse("fb222222-2222-2222-2222-222222222222"), Name = "Rolls Material", PlantId = plant2Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = Guid.Parse("f1222222-2222-2222-2222-222222222222"), WorkCenterGroupId = grpRollsMaterial },
            new WorkCenter { Id = Guid.Parse("fc222222-2222-2222-2222-222222222222"), Name = "Fitup Queue", PlantId = plant2Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = Guid.Parse("f5222222-2222-2222-2222-222222222222"), WorkCenterGroupId = grpFitupQueue },
            new WorkCenter { Id = Guid.Parse("f1333333-3333-3333-3333-333333333333"), Name = "Rolls 1", PlantId = plant3Id, WorkCenterTypeId = wctRollsId, ProductionLineId = line1Plt3, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpRolls1 },
            new WorkCenter { Id = Guid.Parse("f2333333-3333-3333-3333-333333333333"), Name = "Long Seam 1", PlantId = plant3Id, WorkCenterTypeId = wctLongSeamId, ProductionLineId = line1Plt3, NumberOfWelders = 1, DataEntryType = "standard", WorkCenterGroupId = grpLongSeam1 },
            new WorkCenter { Id = Guid.Parse("f3333333-3333-3333-3333-333333333333"), Name = "Long Seam Inspection", PlantId = plant3Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = "inspection", WorkCenterGroupId = grpLongSeamInsp },
            new WorkCenter { Id = Guid.Parse("f4333333-3333-3333-3333-333333333333"), Name = "RT X-ray Queue", PlantId = plant3Id, WorkCenterTypeId = wctXrayId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpRtXrayQueue },
            new WorkCenter { Id = Guid.Parse("f5333333-3333-3333-3333-333333333333"), Name = "Fitup", PlantId = plant3Id, WorkCenterTypeId = wctFitupId, ProductionLineId = line1Plt3, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpFitup },
            new WorkCenter { Id = Guid.Parse("f6333333-3333-3333-3333-333333333333"), Name = "Round Seam", PlantId = plant3Id, WorkCenterTypeId = wctRoundSeamId, ProductionLineId = line1Plt3, NumberOfWelders = 1, DataEntryType = null, WorkCenterGroupId = grpRoundSeam },
            new WorkCenter { Id = Guid.Parse("f7333333-3333-3333-3333-333333333333"), Name = "Round Seam Inspection", PlantId = plant3Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = "inspection", WorkCenterGroupId = grpRoundSeamInsp },
            new WorkCenter { Id = Guid.Parse("f8333333-3333-3333-3333-333333333333"), Name = "Spot X-ray", PlantId = plant3Id, WorkCenterTypeId = wctSpotXrayId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpSpotXray },
            new WorkCenter { Id = Guid.Parse("f9333333-3333-3333-3333-333333333333"), Name = "Nameplate", PlantId = plant3Id, WorkCenterTypeId = wctNameplateId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpNameplate },
            new WorkCenter { Id = Guid.Parse("fa333333-3333-3333-3333-333333333333"), Name = "Hydro", PlantId = plant3Id, WorkCenterTypeId = wctHydroId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null, WorkCenterGroupId = grpHydro },
            new WorkCenter { Id = Guid.Parse("fb333333-3333-3333-3333-333333333333"), Name = "Rolls Material", PlantId = plant3Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = Guid.Parse("f1333333-3333-3333-3333-333333333333"), WorkCenterGroupId = grpRollsMaterial },
            new WorkCenter { Id = Guid.Parse("fc333333-3333-3333-3333-333333333333"), Name = "Fitup Queue", PlantId = plant3Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = Guid.Parse("f5333333-3333-3333-3333-333333333333"), WorkCenterGroupId = grpFitupQueue }
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
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888804"), EmployeeNumber = "EMP005", FirstName = "Lisa", LastName = "Chen", DisplayName = "Lisa Chen", RoleTier = 4.0m, RoleName = "Supervisor", DefaultSiteId = plant1Id, IsCertifiedWelder = false, RequirePinForLogin = false, PinHash = null }
        );

        context.Vendors.AddRange(
            new Vendor { Id = Guid.Parse("51000001-0000-0000-0000-000000000001"), Name = "Nucor Steel", VendorType = "mill", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("51000002-0000-0000-0000-000000000002"), Name = "Steel Dynamics", VendorType = "mill", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("51000003-0000-0000-0000-000000000003"), Name = "NLMK", VendorType = "mill", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("52000001-0000-0000-0000-000000000001"), Name = "Metals USA", VendorType = "processor", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("52000002-0000-0000-0000-000000000002"), Name = "Steel Technologies", VendorType = "processor", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("53000001-0000-0000-0000-000000000001"), Name = "CMF Inc", VendorType = "head", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("53000002-0000-0000-0000-000000000002"), Name = "Compco Industries", VendorType = "head", SiteCode = null, IsActive = true }
        );

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
            new CharacteristicWorkCenter { Id = Guid.Parse("c1000001-0000-0000-0000-000000000001"), CharacteristicId = charLongSeamId, WorkCenterId = wcLongSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c1000001-0000-0000-0000-000000000002"), CharacteristicId = charLongSeamId, WorkCenterId = Guid.Parse("f3222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c1000001-0000-0000-0000-000000000003"), CharacteristicId = charLongSeamId, WorkCenterId = Guid.Parse("f3333333-3333-3333-3333-333333333333") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000001-0000-0000-0000-000000000001"), CharacteristicId = charRs1Id, WorkCenterId = wcRoundSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000001-0000-0000-0000-000000000002"), CharacteristicId = charRs1Id, WorkCenterId = Guid.Parse("f7222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000001-0000-0000-0000-000000000003"), CharacteristicId = charRs1Id, WorkCenterId = Guid.Parse("f7333333-3333-3333-3333-333333333333") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000002-0000-0000-0000-000000000001"), CharacteristicId = charRs2Id, WorkCenterId = wcRoundSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000002-0000-0000-0000-000000000002"), CharacteristicId = charRs2Id, WorkCenterId = Guid.Parse("f7222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000002-0000-0000-0000-000000000003"), CharacteristicId = charRs2Id, WorkCenterId = Guid.Parse("f7333333-3333-3333-3333-333333333333") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000003-0000-0000-0000-000000000001"), CharacteristicId = charRs3Id, WorkCenterId = wcRoundSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000003-0000-0000-0000-000000000002"), CharacteristicId = charRs3Id, WorkCenterId = Guid.Parse("f7222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000003-0000-0000-0000-000000000003"), CharacteristicId = charRs3Id, WorkCenterId = Guid.Parse("f7333333-3333-3333-3333-333333333333") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000004-0000-0000-0000-000000000001"), CharacteristicId = charRs4Id, WorkCenterId = wcRoundSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000004-0000-0000-0000-000000000002"), CharacteristicId = charRs4Id, WorkCenterId = Guid.Parse("f7222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000004-0000-0000-0000-000000000003"), CharacteristicId = charRs4Id, WorkCenterId = Guid.Parse("f7333333-3333-3333-3333-333333333333") }
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
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 }
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
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000001"), Name = "Rolls 1 Asset", WorkCenterId = wcRolls1Plt1, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000002"), Name = "Long Seam 1 Asset", WorkCenterId = wcLongSeam1Plt1, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000003"), Name = "Fitup Asset", WorkCenterId = wcFitupPlt1, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000004"), Name = "Round Seam Asset", WorkCenterId = wcRoundSeamPlt1, ProductionLineId = line1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000005"), Name = "Hydro Asset", WorkCenterId = wcHydroPlt1, ProductionLineId = line1Plt1 }
        );

        context.SaveChanges();
    }
}
