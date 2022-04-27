using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixCommon.Threading.Dispatcher.Actions
{
    public class MessagePumpActionFireForget : MessagePumpActionBase
    {
        private readonly Action<CancellationToken> _action;

        public MessagePumpActionFireForget(Action<CancellationToken> action) 
            : base(CancellationToken.None)
        {
            _action = action;
        }

        internal override void SetFailed(Exception e)
        {
            // noop
        }

        internal override void SetCanceled()
        {
            // noop
        }

        protected override void Execute(CancellationToken cancellationToken)
        {
            _action.Invoke(cancellationToken);
        }
    }
}
