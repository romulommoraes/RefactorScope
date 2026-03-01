using RefactorScope.Core.Configuration;

public static class DumpStrategyResolver
{
    public static IDumpStrategy Resolve(RefactorScopeConfig config)
    {
        if (config.DumpStrategy.Mode == "global")
            return new GlobalDumpStrategy();

        if (config.DumpStrategy.Mode == "segmented")
        {
            var segmentation = ResolveSegmentation(config);
            return new SegmentedDumpStrategy(segmentation);
        }

        throw new Exception("Invalid dumpStrategy.mode");
    }

    private static ISegmentationResolver ResolveSegmentation(RefactorScopeConfig config)
    {
        var splitBy = config.DumpStrategy.SplitBy ?? "topFolder";

        return splitBy switch
        {
            "topFolder" => new TopFolderSegmentationResolver(),
            "layer" => new LayerSegmentationResolver(),
            _ => throw new Exception("Invalid dumpStrategy.splitBy")
        };
    }
}