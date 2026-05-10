using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Microtone.Models;
using Microtone.Models.DiagramTimeline.Rendering;
using Microtone.Models.Rendering;
using Microtone.Models.Rendering.HitTest;
using SkiaSharp;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Input;

namespace Microtone.Controls
{
    public class DiagramTimeline : Control
    {
        Point _mouse;
        Point? _lastDrag;
        Point? _dragStartWorld;

        private readonly HitTestManager hitTestManager = new(); 

        private bool _initialized;
        private bool _isDraggingObject;

        //bindできるものを定義
        public static readonly StyledProperty<SKRenderData?> RenderDataProperty =
            AvaloniaProperty.Register<DiagramTimeline, SKRenderData?>(nameof(RenderData));
        public SKRenderData? RenderData
        {
            get => GetValue(RenderDataProperty);
            set => SetValue(RenderDataProperty, value);
        }

        public static readonly StyledProperty<SKPoint> ScaleProperty =
            AvaloniaProperty.Register<DiagramTimeline, SKPoint>(nameof(Scale), new(1,1));
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
            AvaloniaProperty.Register<DiagramTimeline, HitInfo?>(nameof(PointerHit), null, defaultBindingMode: Avalonia.Data.BindingMode.OneWayToSource);
        public HitInfo? PointerHit
        {
            get => GetValue(PointerHitProperty);
            set => SetValue(PointerHitProperty, value);
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

        public static readonly StyledProperty<ICommand> OnRightClickedCommandProperty = 
            AvaloniaProperty.Register<DiagramTimeline, ICommand>(nameof(OnRightClickedCommand));
        public ICommand OnRightClickedCommand
        {
            get => GetValue(OnRightClickedCommandProperty);
            set => SetValue(OnRightClickedCommandProperty, value);
        }


        //コンストラクタ
        public DiagramTimeline()
        {
            RenderData ??= new();
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

            public CustomDrawOp(Rect bounds, Point offset, SKPoint scale, SKRenderData? data, HitTestManager hittest)
            {
                _bounds = bounds;
                _offset = offset;
                _scale = scale;
                _data = data ?? new SKRenderData();
                _hitTestManager = hittest;
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

                foreach (var item in _data.Commands)
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
                _mouse = e.GetPosition(this);
                RefreshPointerPosition();
            }
            var worldPos = new SKPoint((float)PointerPosition.X, (float)PointerPosition.Y);
            var hit = hitTestManager.HitTest(worldPos);
            OnPressedCommand?.Execute(hit);

            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;

            e.Pointer.Capture(this);
            _lastDrag = e.GetPosition(this);
            _dragStartWorld = new(worldPos.X, worldPos.Y);
            _isDraggingObject = hit != null && hit?.Kind != HitKind.None;
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            var p = e.GetPosition(this);
            _mouse = p;
            PointerHit = hitTestManager.HitTest(new((float)PointerPosition.X,(float)PointerPosition.Y));
            RefreshPointerPosition();
            if (_lastDrag != null)
            {
                var delta = p - _lastDrag.Value;

                if (_isDraggingObject && _dragStartWorld != null)
                {
                    // ワールド座標デルタに変換して ViewModel へ
                    DragInfo info = new() { 
                        DragStart = (Point)_dragStartWorld!,
                        DragEnd = PointerPosition,
                        Delta = delta
                    };
                    
                    OnDragCommand?.Execute(info);
                }
                else
                {
                    // 従来のビュー全体スクロール
                    Offset += delta;
                    InvalidateVisual();
                }
                _lastDrag = p;
            }
            //if (_lastDrag != null)
            //{
            //    Offset += p - _lastDrag.Value;
            //    _lastDrag = p;
            //    InvalidateVisual();
            //}
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_isDraggingObject)
                OnDragReleasedCommand?.Execute(null);
            _lastDrag = null;
            _isDraggingObject = false;
            _dragStartWorld = null;
            e.Pointer.Capture(null);

            //var props = e.GetCurrentPoint(this).Properties;
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                OnRightClickedCommand?.Execute(PointerPosition);
                ContextMenu?.Open(this);
                return;
            }
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
                    (float)screen.X - (worldBefore.X) * Scale.X,
                    (float)screen.Y - (worldBefore.Y) * Scale.Y
                );
                InvalidateVisual();
            }else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                OnScaleChangedCommand?.Execute(e.Delta.Y);
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

        //バインド内容の監視
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == RenderDataProperty)
            {
                RenderDataChanged();
            }else if (change.Property == BoundsProperty && change.OldValue != null && change.NewValue != null)
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
            }else if (change.Property == ScaleProperty || change.Property == OffsetProperty)
            {
                InvalidateVisual();
                RefreshPointerPosition();
            }
        }

        private void RenderDataChanged() { InvalidateVisual(); }

    }
}
