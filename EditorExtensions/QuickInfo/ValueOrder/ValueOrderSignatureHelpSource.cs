﻿using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    internal class ValueOrderSignatureHelpSource : ISignatureHelpSource
    {
        private readonly ITextBuffer _buffer;

        public ValueOrderSignatureHelpSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
            ParseItem item = document.StyleSheet.ItemBeforePosition(point.Value.Position);

            if (item == null)
                return;

            Declaration dec = item.FindType<Declaration>();
            if (dec == null || dec.PropertyName == null || dec.Colon == null)
                return;

            var span = _buffer.CurrentSnapshot.CreateTrackingSpan(dec.Colon.Start, dec.Length - dec.PropertyName.Length, SpanTrackingMode.EdgeNegative);

            ValueOrderFactory.AddSignatures method = ValueOrderFactory.GetMethod(dec);

            if (method != null)
            {
                signatures.Clear();
                method(session, signatures, dec, span);

                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => {
                        session.Properties.AddProperty("dec", dec);
                        session.Match();
                    }), 
                    DispatcherPriority.Normal, null);
            }
        }

        public ISignature GetBestMatch(ISignatureHelpSession session)
        {
            int number = 0;

            if (session.Properties.ContainsProperty("dec"))
            {
                Declaration dec = session.Properties["dec"] as Declaration;
                string methodName = ValueOrderFactory.GetMethod(dec).Method.Name;
                if (dec.Values.Count > 0 && (methodName == "Margins" || methodName == "Corners"))
                {
                    number = 4 - dec.Values.Count;
                }
            }

            return (session.Signatures != null && session.Signatures.Count > number && number > -1)
                ? session.Signatures[number]
                : null;
        }

        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
    }
}
