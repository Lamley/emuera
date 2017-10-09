using System.Windows.Forms;

namespace MinorShift.Emuera.Forms
{
    internal sealed class EraPictureBox : PictureBox
    {
        public void SetStyle()
        {
            //if (StaticConfig.UseImageBuffer)
            //{
            //    this.SetStyle(ControlStyles.Opaque, true);
            //    this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            //    this.SetStyle(ControlStyles.UserPaint, true);
            //    this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            //    this.SetStyle(ControlStyles.ResizeRedraw, false);
            //}
            //else
            //{
            //    this.SetStyle(ControlStyles.Opaque, false);
            //    this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            //    this.SetStyle(ControlStyles.UserPaint, true);
            //    this.SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
            //    this.SetStyle(ControlStyles.ResizeRedraw, false);
            //}
            //背景描画カット
            SetStyle(ControlStyles.Opaque, true);
            //以下3つでダブルバッファリング
            //ただしOnPaintかPaintイベントのe.Graphicsを使用する場合のみ
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            //リサイズ時に自動再描画
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            //SetStyle(ControlStyles.Opaque, true);だとそもそもここにはこない
            //SetStyleを適切にやれば普通のPictureBoxでよかった
            //base.OnPaintBackground(pevent);
        }
    }
}