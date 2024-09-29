namespace Perpetuum.Collections.Spatial
{
    public abstract class Cell
    {
        protected Cell(Area boundingBox)
        {
            BoundingBox = boundingBox;
        }

        public Area BoundingBox { get; }

        public override string ToString()
        {
            return string.Format("BoundingBox: {0}", BoundingBox);
        }
    }
}