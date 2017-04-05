namespace MysteryCommand.Procedural.Mesh
{
	public class Square {

        public ControlNode tl, tr, br, bl;
        public Node tc, mr, bc, ml;
        public int config = 0;

        public Square (ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft) {
            tl = topLeft;
            tr = topRight;
            br = bottomRight;
            bl = bottomLeft;

            tc = tl.right;
            mr = br.above;
            bc = bl.right;
            ml = bl.above;

            if (tl.active) config += 8;
            if (tr.active) config += 4;
            if (br.active) config += 2;
            if (bl.active) config += 1;
        }

    }
}