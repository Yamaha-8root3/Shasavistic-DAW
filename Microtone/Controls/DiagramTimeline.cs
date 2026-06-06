using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Microtone.Models.Rendering;
using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Windows.Input;

namespace Microtone.Controls
{
  public class DiagramTimeline : Control
  {
    private Point _mouse;
    private Point? _lastDrag;
    private Point? _dragStartWorld;
    private Point? _dragStartScreen;

    private readonly HitTestManager hitTestManager = new();
    private SKRenderData? _prevRenderData;

    private bool _initialized;
    private bool _isDraggingObject;

    private HitInfo? _lastPressedHit;

    public record ClickedInfo(HitInfo? Hit, Point WorldPos, MouseButton Button, KeyModifiers Modifiers);
    public record DragInfo
    {
      public required ClickedInfo PressedInfo;
      public Point DragStart;
      public Point DragEnd;
      public Point DragDelta => DragEnd - DragStart;
      //イベント間の一瞬の移動量 全体の移動量ではない
      public Point Delta;
    }

    //bindできるものを定義
    public static readonly StyledProperty<SKRenderData?> RenderDataProperty =
        AvaloniaProperty.Register<DiagramTimeline, SKRenderData?>(nameof(RenderData));
    public SKRenderData? RenderData
    {
      get => GetValue(RenderDataProperty);
      set => SetValue(RenderDataProperty, value);
    }

    public static readonly StyledProperty<SKPoint> ScaleProperty =
        AvaloniaProperty.Register<DiagramTimeline, SKPoint>(nameof(Scale), new(1, 1));
    public SKPoint Scale
    {
      get => GetValue(ScaleProperty);
      set => SetValue(ScaleProperty, value);
    }

    public static readonly StyledProperty<Point> OffsetProperty =
        AvaloniaProperty.Register<DiagramTimeline, Point>(nameof(Offset), new(0, 0));
    public Point Offset
    {
      get => GetValue(OffsetProperty);
      set => SetValue(OffsetProperty, value);
    }

    public static readonly StyledProperty<Point> PointerPositionProperty =
        AvaloniaProperty.Register<DiagramTimeline, Point>(nameof(Offset), new(0, 0), defaultBindingMode: Avalonia.Data.BindingMode.OneWayToSource);
    public Point PointerPosition
    {
      get => GetValue(PointerPositionProperty);
      set => SetValue(PointerPositionProperty, value);
    }

    public static readonly StyledProperty<HitInfo?> PointerHitProperty =
        AvaloniaProperty.Register<DiagramTimeline, HitInfo?>(nameof(PointerHit), defaultBindingMode: Avalonia.Data.BindingMode.OneWayToSource);
    public HitInfo? PointerHit
    {
      get => GetValue(PointerHitProperty);
      set => SetValue(PointerHitProperty, value);
    }

    public static readonly StyledProperty<Guid?> SelectedItemIdProperty =
    AvaloniaProperty.Register<DiagramTimeline, Guid?>(nameof(SelectedItemId));
    public Guid? SelectedItemId
    {
      get => GetValue(SelectedItemIdProperty);
      set => SetValue(SelectedItemIdProperty, value);
    }

    public static readonly StyledProperty<Rect?> SelectedHitBoundScreenProperty =
        AvaloniaProperty.Register<DiagramTimeline, Rect?>(nameof(SelectedHitBoundScreen),
            defaultBindingMode: Avalonia.Data.BindingMode.OneWayToSource);
    public Rect? SelectedHitBoundScreen
    {
      get => GetValue(SelectedHitBoundScreenProperty);
      private set => SetValue(SelectedHitBoundScreenProperty, value);
    }

    //コマンドの定義
    public static readonly StyledProperty<ICommand> OnScaleChangedCommandProperty =
            AvaloniaProperty.Register<DiagramTimeline, ICommand>(nameof(OnScaleChangedCommand));
    public ICommand OnScaleChangedCommand
    {
      get => GetValue(OnScaleChangedCommandProperty);
      set => SetValue(OnScaleChangedCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> OnPressedCommandProperty =
        AvaloniaProperty.Register<DiagramTimeline, ICommand>(nameof(OnPressedCommand));
    public ICommand OnPressedCommand
    {
      get => GetValue(OnPressedCommandProperty);
      set => SetValue(OnPressedCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> OnDragCommandProperty =
        AvaloniaProperty.Register<DiagramTimeline, ICommand>(nameof(OnDragCommand));
    public ICommand OnDragCommand
    {
      get => GetValue(OnDragCommandProperty);
      set => SetValue(OnDragCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> OnDragReleasedCommandProperty =
        AvaloniaProperty.Register<DiagramTimeline, ICommand>(nameof(OnDragReleasedCommand));
    public ICommand OnDragReleasedCommand
    {
      get => GetValue(OnDragReleasedCommandProperty);
      set => SetValue(OnDragReleasedCommandProperty, value);
    }

    //public static readonly StyledProperty<ICommand> OnRightClickedCommandProperty =
    //    AvaloniaProperty.Register<DiagramTimeline, ICommand>(nameof(OnRightClickedCommand));
    //public ICommand OnRightClickedCommand
    //{
    //  get => GetValue(OnRightClickedCommandProperty);
    //  set => SetValue(OnRightClickedCommandProperty, value);
    //}

    public static readonly StyledProperty<ICommand> OnClickedCommandProperty =
        AvaloniaProperty.Register<DiagramTimeline, ICommand>(nameof(OnClickedCommand));
    public ICommand OnClickedCommand
    {
      get => GetValue(OnClickedCommandProperty);
      set => SetValue(OnClickedCommandProperty, value);
    }


    //コンストラクタ
    public DiagramTimeline()
    {
      RenderData ??= new();
      RenderData.CursorChanged += () => InvalidateVisual();
    }


    public override void Render(DrawingContext context)
    {
      //System.Diagnostics.Debug.WriteLine("Render called");
      context.FillRectangle(Brushes.Transparent, Bounds);
      base.Render(context);
      context.Custom(new CustomDrawOp(Bounds, Offset, Scale, RenderData, hitTestManager));
    }

    private class CustomDrawOp : ICustomDrawOperation
    {
      private readonly Rect _bounds;
      private readonly Point _offset;
      private readonly SKPoint _scale;
      private readonly SKRenderData _data;
      private readonly HitTestManager _hitTestManager;
      private readonly SKRenderCommand[] _commands;

      public CustomDrawOp(Rect bounds, Point offset, SKPoint scale, SKRenderData? data, HitTestManager hittest
                          )
      {
        _bounds = bounds;
        _offset = offset;
        _scale = scale;
        _data = data ?? new SKRenderData();
        _hitTestManager = hittest;
        _commands = [.. _data.Commands]; // 処理中の置き換え防止
      }


      public void Dispose() { }
      public Rect Bounds => _bounds;
      public bool HitTest(Point p) => false;
      public bool Equals(ICustomDrawOperation? other)
      {
        return false;
      }

      public void Render(ImmediateDrawingContext context)
      {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature == null) return;

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;

        canvas.Save();

        canvas.ClipRect(new SKRect(
                        0,
                        0,
                        (float)_bounds.Width,
                        (float)_bounds.Height));

        //背景描画
        //canvas.DrawRect(
        //    (float)_bounds.X,
        //    (float)_bounds.Y,
        //    (float)_bounds.Width,
        //    (float)_bounds.Height,
        //    _data.Background);
        canvas.Clear(_data.Background.Color);
        _hitTestManager.Clear();

        // サイズ変更及び移動
        canvas.Translate((float)_offset.X, (float)_offset.Y);
        canvas.Scale(_scale);

        foreach (var item in _commands)
        {
          HitInfo? hitinfo = item.Render(canvas);
          if (hitinfo != null)
          {
            _hitTestManager.Register(hitinfo);
          }
        }

        canvas.Restore();


      }

    }
    // サイズを調整させる
    protected override Size ArrangeOverride(Size finalSize)
    {

      if (!_initialized)
      {
        Offset = new Point(
            finalSize.Width / 2,
            finalSize.Height / 2
        );
        _initialized = true;
      }

      return base.ArrangeOverride(finalSize);
    }

    // マウス操作
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
      if (ContextMenu?.IsOpen == true)
      {
        ContextMenu.Close();
      }
      _mouse = e.GetPosition(this);
      RefreshPointerPosition();
      var worldPos = new SKPoint((float)PointerPosition.X, (float)PointerPosition.Y);
      var hit = hitTestManager.HitTest(worldPos);
      _lastPressedHit = hit;
      OnPressedCommand.Execute(new ClickedInfo(hit, PointerPosition, GetMouseButton(e), e.KeyModifiers));

      if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;

      e.Pointer.Capture(this);
      _lastDrag = e.GetPosition(this);
      _dragStartScreen = e.GetPosition(this);
      _dragStartWorld = new(worldPos.X, worldPos.Y);
      _isDraggingObject = hit != null && hit.Kind != HitKind.None;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
      var p = e.GetPosition(this);
      _mouse = p;
      RefreshPointerPosition();
      PointerHit = hitTestManager.HitTest(new((float)PointerPosition.X, (float)PointerPosition.Y));
      if (_lastDrag != null)
      {
        var delta = p - _lastDrag.Value;

        if (_isDraggingObject && _dragStartWorld != null)
        {
          // ワールド座標デルタに変換して ViewModel へ
          DragInfo info = new()
          {
            PressedInfo = new ClickedInfo(
                _lastPressedHit,
                _dragStartWorld.Value,
                MouseButton.Left,
                e.KeyModifiers
            ),
            DragStart = _dragStartWorld.Value,
            DragEnd = PointerPosition,
            Delta = new Point(
                (PointerPosition.X - _dragStartWorld.Value.X),
                (PointerPosition.Y - _dragStartWorld.Value.Y)
            )
          };

          OnDragCommand.Execute(info);
        }
        else
        {
          // 従来のビュー全体スクロール
          Offset += delta;
          InvalidateVisual();
        }
        _lastDrag = p;
      }
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
      var didDrag =
        _dragStartScreen.HasValue && ( e.GetPosition(this) - _dragStartScreen.Value).ToSKPoint().Length > 5;
      if (!didDrag)
      {
        OnClickedCommand.Execute(new ClickedInfo(
            _lastPressedHit,
            PointerPosition,
            e.InitialPressMouseButton,
            e.KeyModifiers
        ));
      }
      else
        OnDragReleasedCommand.Execute(null);

      if (e.InitialPressMouseButton == MouseButton.Right)
        ContextMenu?.Open();

      _lastDrag = null;
      _isDraggingObject = false;
      _dragStartWorld = null;
      _dragStartScreen = null;
      e.Pointer.Capture(null);
    }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
      if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
      {
        float zoom = e.Delta.Y > 0 ? 1.1f : 0.9f;
        var mouse = e.GetPosition(this);
        // スクリーン座標（ピクセル）
        var screen = new SKPoint((float)mouse.X, (float)mouse.Y);

        // 拡大前のワールド座標を正しく計算:
        // Render 側では Translate(offset) のあと Scale(scale) をしている
        // 
        var worldBefore = new SKPoint(
            (float)(screen.X - Offset.X) / Scale.X,
            (float)(screen.Y - Offset.Y) / Scale.Y
        );

        // スケール更新
        Scale = new SKPoint(Scale.X * zoom, Scale.Y * zoom);

        // スケール更新後、同じワールド座標が同じスクリーン位置になるようにオフセットを再計算する:
        // world = (mouse - offset) / scaleの変形
        Offset = new Point(
            screen.X - (worldBefore.X) * Scale.X,
            screen.Y - (worldBefore.Y) * Scale.Y
        );
        InvalidateVisual();
      }
      else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
      {
        OnScaleChangedCommand.Execute(e.Delta.Y);
      }
      else
      {
        Offset = new Point(Offset.X + e.Delta.Y * 50, Offset.Y);
        InvalidateVisual();
      }
    }

    private void RefreshPointerPosition()
    {
      PointerPosition = new Point((_mouse.X - Offset.X) / Scale.X, (_mouse.Y - Offset.Y) / Scale.Y);
    }
    private void RefreshSelectedHitBoundScreen()
    {
      if (SelectedItemId == null) { SelectedHitBoundScreen = null; return; }
      var hit = hitTestManager.GetHitInfo(SelectedItemId.Value);
      if (hit == null) return;

      var topLevel = TopLevel.GetTopLevel(this);
      if (topLevel == null) return;

      // ワールド→コントロール相対座標
      var ctrlTL = new Point(
          hit.Bounds.Left * Scale.X + Offset.X,
          hit.Bounds.Top * Scale.Y + Offset.Y);
      var ctrlBR = new Point(
          hit.Bounds.Right * Scale.X + Offset.X,
          hit.Bounds.Bottom * Scale.Y + Offset.Y);
      

      SelectedHitBoundScreen = new Rect(
          ctrlTL,ctrlBR);

    }
    private MouseButton GetMouseButton(PointerPressedEventArgs EventArgs)
    {
      var properties = EventArgs.GetCurrentPoint(this).Properties;
      if (properties.IsLeftButtonPressed) return MouseButton.Left;
      if (properties.IsRightButtonPressed) return MouseButton.Right;
      if (properties.IsMiddleButtonPressed) return MouseButton.Middle;
      return MouseButton.None; // デフォルト
    }

    //バインド内容の監視
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
      base.OnPropertyChanged(change);

      if (change.Property == RenderDataProperty)
      {
        RenderDataChanged();
        RefreshSelectedHitBoundScreen();
      }
      else if (change.Property == BoundsProperty && change.OldValue != null && change.NewValue != null)
      {
        var oldBounds = (Rect)change.OldValue;
        var newBounds = (Rect)change.NewValue;

        if (oldBounds.Size == default) return;

        // 画面中央にあるワールド座標を維持する
        var oldCenter = oldBounds.Center;
        var newCenter = newBounds.Center;

        // 新しいオフセットを計算： newCenter / scale - worldAtOldCenter
        Offset -= new Point((oldCenter.X - newCenter.X) / Scale.X, (oldCenter.Y - newCenter.Y) / Scale.Y);

        InvalidateVisual();
      }
      else if (change.Property == ScaleProperty || change.Property == OffsetProperty)
      {
        InvalidateVisual();
        RefreshPointerPosition();
        RefreshSelectedHitBoundScreen();
      }
      else if (change.Property == SelectedItemIdProperty)
      {
        RefreshSelectedHitBoundScreen();
      }
    }

    private void RenderDataChanged()
    {
      _prevRenderData?.CursorChanged -= InvalidateVisual;

      _prevRenderData = RenderData;

      RenderData?.CursorChanged += InvalidateVisual;
      InvalidateVisual();
    }

  }
}
