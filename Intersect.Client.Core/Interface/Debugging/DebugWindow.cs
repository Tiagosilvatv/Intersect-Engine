using Intersect.Async;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.Data;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.Framework.Gwen.Control.Utility;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Extensions;
using static Intersect.Client.Framework.File_Management.GameContentManager;

namespace Intersect.Client.Interface.Debugging;

internal sealed partial class DebugWindow : Window
{
    private readonly List<ICancellableGenerator> _generators;
    private readonly GameFont? _defaultFont;
    private bool _wasParentDrawDebugOutlinesEnabled;
    private bool _drawDebugOutlinesEnabled;

    public DebugWindow(Base parent) : base(parent, Strings.Debug.Title, false, nameof(DebugWindow))
    {
        _generators = [];
        DisableResizing();
        MinimumSize = new Point(320, 320);

        _defaultFont = Current?.GetFont("sourcesansproblack", 10);

        Tabs = CreateTabs();

        TabInfo = Tabs.AddPage(Strings.Debug.TabLabelInfo);
        CheckboxDrawDebugOutlines = CreateInfoCheckboxDrawDebugOutlines(TabInfo.Page);
        CheckboxEnableLayoutHotReloading = CreateInfoCheckboxEnableLayoutHotReloading(TabInfo.Page);
        ButtonShutdownServer = CreateInfoButtonShutdownServer(TabInfo.Page);
        ButtonShutdownServerAndExit = CreateInfoButtonShutdownServerAndExit(TabInfo.Page);
        TableDebugStats = CreateInfoTableDebugStats(TabInfo.Page);

        TabAssets = Tabs.AddPage(Strings.Debug.TabLabelAssets);
        AssetsToolsTable = CreateAssetsToolsTable(TabAssets.Page);
        AssetsList = CreateAssetsList(TabAssets.Page);
        AssetsButtonReloadAsset = CreateAssetsButtonReloadAsset(AssetsToolsTable, AssetsList);

        AssetsToolsTable.SizeToChildren();

        IsHidden = true;
    }

    private SearchableTree CreateAssetsList(Base parent)
    {
        var dataProvider = new TexturesSearchableTreeDataProvider(Current, this);
        SearchableTree assetList = new(parent, dataProvider)
        {
            Dock = Pos.Fill,
            FontSearch = _defaultFont,
            FontTree = _defaultFont,
            Margin = new Margin(0, 4, 0, 0),
            SearchPlaceholderText = Strings.Debug.AssetsSearchPlaceholder,
        };

        return assetList;
    }

    private Table CreateAssetsToolsTable(Base assetsTabPage)
    {
        Table table = new(assetsTabPage, nameof(AssetsToolsTable))
        {
            ColumnCount = 2,
            Dock = Pos.Top,
        };

        return table;
    }

    private Button CreateAssetsButtonReloadAsset(Table table, SearchableTree assetList)
    {
        var row = table.AddRow();

        Button buttonReloadAsset = new(row, name: nameof(AssetsButtonReloadAsset))
        {
            IsDisabled = assetList.SelectedNodes.Length < 1,
            Font = _defaultFont,
            Text = Strings.Debug.ReloadAsset,
        };

        assetList.SelectionChanged += (_, _) =>
            buttonReloadAsset.IsDisabled = assetList.SelectedNodes.All(
                node => node.UserData is not SearchableTreeDataEntry { UserData: GameTexture }
            );

        buttonReloadAsset.Clicked += (_, _) =>
        {
            foreach (var node in assetList.SelectedNodes)
            {
                if (node.UserData is not SearchableTreeDataEntry entry)
                {
                    continue;
                }

                if (entry.UserData is not IAsset asset)
                {
                    continue;
                }

                // TODO: Audio reloading?
                if (asset is not GameTexture texture)
                {
                    continue;
                }

                texture.Reload();
            }
        };
        row.SetCellContents(0, buttonReloadAsset, enableMouseInput: true);

        return buttonReloadAsset;
    }

    private TabControl Tabs { get; }

    private TabButton TabInfo { get; }

    private TabButton TabAssets { get; }

    private SearchableTree AssetsList { get; }

    private Table AssetsToolsTable { get; }

    private Button AssetsButtonReloadAsset { get; }

    private LabeledCheckBox CheckboxDrawDebugOutlines { get; }

    private LabeledCheckBox CheckboxEnableLayoutHotReloading { get; }

    private Button ButtonShutdownServer { get; }

    private Button ButtonShutdownServerAndExit { get; }

    private Table TableDebugStats { get; }

    protected override void EnsureInitialized()
    {
        LoadJsonUi(UI.Debug, Graphics.Renderer?.GetResolutionString());

        foreach (var generator in _generators)
        {
            generator.Start();
        }
    }

    protected override void OnAttached(Base parent)
    {
        base.OnAttached(parent);

        Root.DrawDebugOutlines = _drawDebugOutlinesEnabled;
    }

    protected override void OnAttaching(Base newParent)
    {
        base.OnAttaching(newParent);
        _wasParentDrawDebugOutlinesEnabled = newParent.DrawDebugOutlines;
    }

    protected override void OnDetached()
    {
        base.OnDetached();
    }

    protected override void OnDetaching(Base oldParent)
    {
        base.OnDetaching(oldParent);
        oldParent.DrawDebugOutlines = _wasParentDrawDebugOutlinesEnabled;
    }

    public override void Dispose()
    {
        foreach (var generator in _generators)
        {
            generator.Dispose();
        }

        base.Dispose();
    }

    private TabControl CreateTabs()
    {
        return new TabControl(this, name: "DebugTabs")
        {
            Dock = Pos.Fill,
        };
    }

    private LabeledCheckBox CreateInfoCheckboxDrawDebugOutlines(Base parent)
    {
        _drawDebugOutlinesEnabled = Root?.DrawDebugOutlines ?? false;

        var checkbox = new LabeledCheckBox(parent, nameof(CheckboxDrawDebugOutlines))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            IsChecked = _drawDebugOutlinesEnabled,
            Text = Strings.Debug.DrawDebugOutlines,
        };

        checkbox.CheckChanged += (_, _) =>
        {
            _drawDebugOutlinesEnabled = checkbox.IsChecked;
            if (Root is { } root)
            {
                root.DrawDebugOutlines = _drawDebugOutlinesEnabled;
            }
        };

        return checkbox;
    }

    private LabeledCheckBox CreateInfoCheckboxEnableLayoutHotReloading(Base parent)
    {
        var checkbox = new LabeledCheckBox(parent, nameof(CheckboxEnableLayoutHotReloading))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            IsChecked = Globals.ContentManager.ContentWatcher.Enabled,
            Text = Strings.Debug.EnableLayoutHotReloading,
            TextColorOverride = Color.Yellow,
        };

        checkbox.CheckChanged += (sender, args) => Globals.ContentManager.ContentWatcher.Enabled = checkbox.IsChecked;

        checkbox.SetToolTipText(Strings.Internals.ExperimentalFeatureTooltip);
        checkbox.ToolTipFont = Skin.DefaultFont;

        return checkbox;
    }

    private Button CreateInfoButtonShutdownServer(Base parent)
    {
        var button = new Button(parent, nameof(ButtonShutdownServer))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            IsHidden = true,
            Text = Strings.Debug.ShutdownServer,
        };

        button.Clicked += (sender, args) =>
        {
            // TODO: Implement
        };

        return button;
    }

    private Button CreateInfoButtonShutdownServerAndExit(Base parent)
    {
        var button = new Button(parent, nameof(ButtonShutdownServerAndExit))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            IsHidden = true,
            Text = Strings.Debug.ShutdownServerAndExit,
        };

        button.Clicked += (sender, args) =>
        {
            // TODO: Implement
        };

        return button;
    }

    private Table CreateInfoTableDebugStats(Base parent)
    {
        var table = new Table(parent, nameof(TableDebugStats))
        {
            ColumnCount = 2,
            Dock = Pos.Fill,
            Font = _defaultFont,
        };

        var fpsProvider = new ValueTableCellDataProvider<int>(() => Graphics.Renderer?.GetFps() ?? 0, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.Fps).Listen(fpsProvider, 1);
        _generators.Add(fpsProvider.Generator);

        var pingProvider = new ValueTableCellDataProvider<int>(() => Networking.Network.Ping, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.Ping).Listen(pingProvider, 1);
        _generators.Add(pingProvider.Generator);

        var drawCallsProvider = new ValueTableCellDataProvider<int>(() => Graphics.DrawCalls, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.Draws).Listen(drawCallsProvider, 1);
        _generators.Add(drawCallsProvider.Generator);

        var mapNameProvider = new ValueTableCellDataProvider<string>(() =>
        {
            var mapId = Globals.Me?.MapId ?? default;

            if (mapId == default)
            {
                return Strings.Internals.NotApplicable;
            }

            return MapInstance.Get(mapId)?.Name ?? Strings.Internals.NotApplicable;
        }, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.Map).Listen(mapNameProvider, 1);
        _generators.Add(mapNameProvider.Generator);

        var coordinateXProvider = new ValueTableCellDataProvider<int>(() => Globals.Me?.X ?? -1, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Internals.CoordinateX).Listen(coordinateXProvider, 1);
        _generators.Add(coordinateXProvider.Generator);

        var coordinateYProvider = new ValueTableCellDataProvider<int>(() => Globals.Me?.Y ?? -1, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Internals.CoordinateY).Listen(coordinateYProvider, 1);
        _generators.Add(coordinateYProvider.Generator);

        var coordinateZProvider = new ValueTableCellDataProvider<int>(() => Globals.Me?.Z ?? -1, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Internals.CoordinateZ).Listen(coordinateZProvider, 1);
        _generators.Add(coordinateZProvider.Generator);

        var knownEntitiesProvider = new ValueTableCellDataProvider<int>(() => Graphics.DrawCalls, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.KnownEntities).Listen(knownEntitiesProvider, 1);
        _generators.Add(knownEntitiesProvider.Generator);

        var knownMapsProvider = new ValueTableCellDataProvider<int>(() => MapInstance.Lookup.Count, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.KnownMaps).Listen(knownMapsProvider, 1);
        _generators.Add(knownMapsProvider.Generator);

        var mapsDrawnProvider = new ValueTableCellDataProvider<int>(() => Graphics.MapsDrawn, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.MapsDrawn).Listen(mapsDrawnProvider, 1);
        _generators.Add(mapsDrawnProvider.Generator);

        var entitiesDrawnProvider = new ValueTableCellDataProvider<int>(() => Graphics.EntitiesDrawn, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.EntitiesDrawn).Listen(entitiesDrawnProvider, 1);
        _generators.Add(entitiesDrawnProvider.Generator);

        var lightsDrawnProvider = new ValueTableCellDataProvider<int>(() => Graphics.LightsDrawn, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.LightsDrawn).Listen(lightsDrawnProvider, 1);
        _generators.Add(lightsDrawnProvider.Generator);

        var timeProvider = new ValueTableCellDataProvider<string>(Time.GetTime, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.Time).Listen(timeProvider, 1);
        _generators.Add(timeProvider.Generator);

        var interfaceObjectsProvider = new ValueTableCellDataProvider<int>(cancellationToken =>
        {
            try
            {
                return Interface.CurrentInterface?.Children.ToArray().SelectManyRecursive(
                    x => x != default ? [.. x.Children] : [],
                    cancellationToken
                ).ToArray().Length ?? 0;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }, waitPredicate: () => Task.FromResult(IsVisible));
        table.AddRow(Strings.Debug.InterfaceObjects).Listen(interfaceObjectsProvider, 1);
        _generators.Add(interfaceObjectsProvider.Generator);

        _ = table.AddRow(Strings.Debug.ControlUnderCursor, 2);

        var controlUnderCursorProvider = new ControlUnderCursorProvider(this);
        // var controlUnderCursorProvider = new ValueTableCellDataProvider<Base>(
        //     Interface.FindControlAtCursor,
        //     waitPredicate: () => Task.FromResult(IsVisible)
        // );
        table.AddRow(Strings.Internals.Type).Listen(controlUnderCursorProvider, 0);
        table.AddRow(Strings.Internals.Name).Listen(controlUnderCursorProvider, 1);
        table.AddRow(Strings.Internals.LocalItem.ToString(Strings.Internals.Bounds)).Listen(controlUnderCursorProvider, 2);
        table.AddRow(Strings.Internals.GlobalItem.ToString(Strings.Internals.Bounds)).Listen(controlUnderCursorProvider, 3);
        table.AddRow(Strings.Internals.Color).Listen(controlUnderCursorProvider, 4);
        table.AddRow(Strings.Internals.ColorOverride).Listen(controlUnderCursorProvider, 5);
        table.AddRow(Strings.Internals.InnerBounds).Listen(controlUnderCursorProvider, 6);
        table.AddRow(Strings.Internals.Margin).Listen(controlUnderCursorProvider, 7);
        table.AddRow(Strings.Internals.Padding).Listen(controlUnderCursorProvider, 8);
        // table.AddRow(Strings.Internals.ColorOverride).Listen(controlUnderCursorProvider, 9);
        // table.AddRow(Strings.Internals.ColorOverride).Listen(controlUnderCursorProvider, 10);
        _generators.Add(controlUnderCursorProvider.Generator);

        var rows = table.Children.OfType<TableRow>().ToArray();
        foreach (var row in rows)
        {
            if (row.GetCellContents(0) is Label label)
            {
                label.TextAlign = Pos.Right | Pos.CenterV;
            }
        }

        return table;
    }

    private partial class ControlUnderCursorProvider : ITableDataProvider
    {
        private readonly Base _owner;

        public ControlUnderCursorProvider(Base owner)
        {
            _owner = owner;
            Generator = new CancellableGenerator<Base>(CreateControlUnderCursorGenerator);
        }

        public event TableDataChangedEventHandler? DataChanged;

        public CancellableGenerator<Base> Generator { get; }

        public void Start()
        {
            _ = Generator;
        }

        private AsyncValueGenerator<Base> CreateControlUnderCursorGenerator(CancellationToken cancellationToken) => new(
            () => WaitForOwnerVisible(cancellationToken).ContinueWith(CreateValue, TaskScheduler.Current),
            HandleValue,
            cancellationToken
        );

        private async Task WaitForOwnerVisible(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);

                if (_owner.IsVisible)
                {
                    return;
                }

                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private static Base CreateValue(Task _) => Interface.FindControlAtCursor();

        private void HandleValue(Base component)
            {
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(
                    0,
                    1,
                    default,
                    component?.GetType().Name ?? Strings.Internals.NotApplicable
                )
            );
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(1, 1, default, component?.CanonicalName ?? string.Empty)
            );
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(2, 1, default, component?.Bounds.ToString() ?? string.Empty)
            );
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(3, 1, default, component?.BoundsGlobal.ToString() ?? string.Empty)
            );
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(
                    4,
                    1,
                    default,
                    (component as IColorableText)?.TextColor ?? string.Empty
                )
            );
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(
                    5,
                    1,
                    default,
                    (component as IColorableText)?.TextColorOverride ?? string.Empty
                )
            );
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(6, 1, default, component?.InnerBounds.ToString() ?? string.Empty)
            );
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(7, 1, default, component?.Margin.ToString() ?? string.Empty)
            );
            DataChanged?.Invoke(
                this,
                new TableDataChangedEventArgs(8, 1, default, component?.Padding.ToString() ?? string.Empty)
            );
        }
    }
}
