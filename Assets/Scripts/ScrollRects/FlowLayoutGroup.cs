using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global

namespace ScrollRects
{
    [AddComponentMenu("Layout/Flow Layout Group", 153)]
    public class FlowLayoutGroup : LayoutGroup
    {
        public enum Corner
        {
            UpperLeft = 0,
            UpperRight = 1,
            LowerLeft = 2,
            LowerRight = 3
        }

        public enum Constraint
        {
            Flexible = 0,
            FixedColumnCount = 1,
            FixedRowCount = 2
        }

        protected Vector2 m_CellSize = new Vector2(100, 100);

        public Vector2 cellSize
        {
            get => m_CellSize;
            set => SetProperty(ref m_CellSize, value);
        }

        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;

        public Vector2 spacing
        {
            get => m_Spacing;
            set => SetProperty(ref m_Spacing, value);
        }


        [SerializeField] protected bool m_Horizontal = true;

        public bool horizontal
        {
            get => m_Horizontal;
            set => SetProperty(ref m_Horizontal, value);
        }

        protected FlowLayoutGroup()
        {
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            const int minColumns = 1;
            int preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(rectChildren.Count));

            RectOffset padding1 = padding;
            SetLayoutInputForAxis(
                padding1.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x,
                padding1.horizontal + (cellSize.x + spacing.x) * preferredColumns - spacing.x,
                -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
//            float width = rectTransform.rect.size.x;
//            int cellCountX = Mathf.Max(1,
//                Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
//            minRows = Mathf.CeilToInt(rectChildren.Count / (float)cellCountX);
            const int minRows = 1;
            float minSpace = padding.vertical + (cellSize.y + spacing.y) * minRows - spacing.y;
            SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis();
        }

        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis();
        }


        private int _cellsPerMainAxis, _actualCellCountX, _actualCellCountY;
        private int _positionX;
        private int _positionY;
        private float _totalWidth;
        private float _totalHeight;

        private float _lastMax;

        private void SetCellsAlongAxis()
        {
            // Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
            // and only vertical values when invoked for the vertical axis.
            // However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
            // Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
            // and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.


            Rect rect = rectTransform.rect;
            float width = rect.size.x;
            float height = rect.size.y;

            int cellCountX;
            if (cellSize.x + spacing.x <= 0)
                cellCountX = int.MaxValue;
            else
                cellCountX = Mathf.Max(1,
                    Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));

            int cellCountY;
            if (cellSize.y + spacing.y <= 0)
                cellCountY = int.MaxValue;
            else
                cellCountY = Mathf.Max(1,
                    Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));

            _cellsPerMainAxis = cellCountX;
            _actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildren.Count);
            _actualCellCountY =
                Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(rectChildren.Count / (float) _cellsPerMainAxis));

            var requiredSpace = new Vector2(
                _actualCellCountX * cellSize.x + (_actualCellCountX - 1) * spacing.x,
                _actualCellCountY * cellSize.y + (_actualCellCountY - 1) * spacing.y
            );
            var startOffset = new Vector2(
                GetStartOffset(0, requiredSpace.x),
                GetStartOffset(1, requiredSpace.y)
            );

            _totalWidth = 0;
            _totalHeight = 0;
            for (var i = 0; i < rectChildren.Count; i++)
            {
                List<RectTransform> children = rectChildren;
                SetChildAlongAxis(children[i], 0, startOffset.x + _totalWidth /*+ currentSpacing[0]*/,
                    children[i].rect.size.x);
                SetChildAlongAxis(rectChildren[i], 1, startOffset.y + _totalHeight /*+ currentSpacing[1]*/,
                    children[i].rect.size.y);

                Vector2 currentSpacing = spacing;

                if (horizontal)
                {
                    _totalWidth += rectChildren[i].rect.width + currentSpacing[0];
                    if (rectChildren[i].rect.height > _lastMax)
                    {
                        _lastMax = rectChildren[i].rect.height;
                    }

                    if (i >= rectChildren.Count - 1)
                        continue;

                    if (!(_totalWidth + rectChildren[i + 1].rect.width + currentSpacing[0] >
                          width - padding.horizontal)) continue;

                    _totalWidth = 0;
                    _totalHeight += _lastMax + currentSpacing[1];
                    _lastMax = 0;
                }
                else
                {
                    _totalHeight += rectChildren[i].rect.height + currentSpacing[1];
                    if (rectChildren[i].rect.width > _lastMax)
                    {
                        _lastMax = rectChildren[i].rect.width;
                    }

                    if (i >= rectChildren.Count - 1)
                        continue;

                    if (!(_totalHeight + rectChildren[i + 1].rect.height + currentSpacing[1] >
                          height - padding.vertical)) continue;

                    _totalHeight = 0;
                    _totalWidth += _lastMax + currentSpacing[0];
                    _lastMax = 0;
                }
            }
        }
    }
}
