### Summary

Classes to aid in multi-threading or task situations.

#### Dtronix.Threading.Dispatcher.ThreadDispatcher (Isolated)

This class and it's supporting classes are for the management of separately executed queued actions.  It allows the specification of the nubmer of threads to create and execute the passed actions on.  It is TPL based so any blocking action or task can be awaited on until completion 

#### Dtronix.Threading.Tasks.ManualResetAwaiterSource & ManualResetAwaiterSource<T> (Isolated)

Performace class which is close to a `TaskCompletionSource`, but less the full task class generation and can be reset for reuse.
