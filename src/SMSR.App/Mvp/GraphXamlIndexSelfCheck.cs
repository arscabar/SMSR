using System.IO;
namespace SMSR.App.Mvp;
internal static class GraphXamlIndexSelfCheck
{
    internal static async Task RunAsync(string root,GraphIndexService index,GraphQueryService query)
    {
        await File.WriteAllTextAsync(Path.Combine(root,"DemoViewModel.cs"),"""
            namespace Demo;
            public class DemoViewModel
            {
                public string Title { get; set; }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root,"DemoView.xaml"),"""
            <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:local="clr-namespace:Demo">
             <Window.DataContext><local:DemoViewModel /></Window.DataContext>
             <TextBlock Text="{Binding Title}" />
            </Window>
            """);
        await index.IndexAsync("graphify-test",root);
        var context=await query.ContextAsync("graphify-test","file:DemoView.xaml");
        foreach(var relation in new[]{"VIEW_MODEL","BINDS_TO"})
            if(!context.Outgoing.Any(n=>n.Edge.Relation==relation&&n.Node.NodeId=="file:DemoViewModel.cs"))
                throw new Exception("XAML file relation projection missing: "+relation);
        var symbol=(await query.SearchAsync("graphify-test","Title",kind:"code")).Nodes.Single(n=>n.Details?.EntityKind=="property");
        if(!(await query.ContextAsync("graphify-test",symbol.NodeId)).Incoming.Any(n=>n.Edge.Relation=="BINDS_TO"))
            throw new Exception("XAML property relation missing");
    }
}
