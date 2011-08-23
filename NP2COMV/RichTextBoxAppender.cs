using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;

namespace NP2COMV
{
    /// <summary>
    /// Description of RichTextBoxAppender.
    /// </summary>
    public class RichTextBoxAppender : AppenderSkeleton
    {
        private RichTextBox _richtextBox;

        private delegate void UpdateControlDelegate(log4net.Core.LoggingEvent loggingEvent);

        private void UpdateControl(log4net.Core.LoggingEvent loggingEvent)
        {
            // I looked at the TortoiseCVS code to figure out how
            // to use the RTB as a colored logger.  It noted a performance
            // problem when the buffer got long, so it cleared it every 100K.
            if (_richtextBox.TextLength > 100000)
            {
                _richtextBox.Clear();
                _richtextBox.SelectionColor = Color.Gray;
                _richtextBox.AppendText("(earlier messages cleared because of log length)\n\n");
            }

            switch (loggingEvent.Level.ToString())
            {
                case "INFO":
                    _richtextBox.SelectionColor = Color.Black;
                    break;
                case "WARN":
                    _richtextBox.SelectionColor = Color.Blue;
                    break;
                case "ERROR":
                    _richtextBox.SelectionColor = Color.Red;
                    break;
                case "FATAL":
                    _richtextBox.SelectionColor = Color.DarkOrange;
                    break;
                case "DEBUG":
                    _richtextBox.SelectionColor = Color.DarkGreen;
                    break;
                default:
                    _richtextBox.SelectionColor = Color.Black;
                    break;
            }

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb)) 
                Layout.Format(sw, loggingEvent);
            _richtextBox.AppendText(sb.ToString());
        }

        protected override void Append(log4net.Core.LoggingEvent loggingEvent)
        {
            // prevent exceptions
            //if (_richtextBox != null && _richtextBox.Created)
            if (_richtextBox != null)
            {
                // make thread safe
                if (_richtextBox.InvokeRequired)
                {
                    _richtextBox.Invoke(
                            new UpdateControlDelegate(UpdateControl),
                            new object[] { loggingEvent });
                }
                else
                {
                    UpdateControl(loggingEvent);
                }
            }
        }


        public RichTextBox RichTextBox
        {
            set
            {
                _richtextBox = value;
            }
            get
            {
                return _richtextBox;
            }
        }

        public static void SetRichTextBox(RichTextBox rtb)
        {
            rtb.ReadOnly = true;
            rtb.HideSelection = false;      // allows rtb to allways append at the end
            rtb.Clear();

            foreach (RichTextBoxAppender appender in GetAppenders().OfType<RichTextBoxAppender>())
            {
                (appender).RichTextBox = rtb;
            }
        }

        private static IEnumerable<IAppender> GetAppenders()
        {
            var appenders = new ArrayList();
            appenders.AddRange(((Hierarchy)LogManager.GetRepository()).Root.Appenders);

            foreach (var log in LogManager.GetCurrentLoggers())
            {
                appenders.AddRange(((Logger)log.Logger).Appenders);
            }

            return (IAppender[])appenders.ToArray(typeof(IAppender));
        }

    }
}
