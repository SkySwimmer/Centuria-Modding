using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem;
using Il2CppSystem.Collections;

namespace FeralTweaks.Actions
{
    /// <summary>
    /// Coroutine injection class
    /// </summary>
    public class FTCoroutine : Object
    {
        public delegate Object CoroutineExecutionDelegate(CoroutineExecutionContext ctx);

        public class CoroutineExecutionContextMemory
        {
            private System.Collections.Generic.Dictionary<string, object> values = new System.Collections.Generic.Dictionary<string, object>();

            /// <summary>
            /// Retrieves values by type
            /// </summary>
            /// <typeparam name="T">Value type</typeparam>
            /// <returns>Value instance</returns>
            public T GetValue<T>()
            {
                string typeN = typeof(T).FullName;
                if (values.ContainsKey(typeN))
                    return (T) values[typeN];
                return default;
            }

            /// <summary>
            /// Retrieves values by type, and registers it if needed
            /// </summary>
            /// <typeparam name="T">Value type</typeparam>
            /// <param name="value">Value to assign</param>
            /// <returns>Value instance</returns>
            public T GetOrCreateValue<T>(T value)
            {
                string typeN = typeof(T).FullName;
                if (values.ContainsKey(typeN))
                    return (T) values[typeN];
                return SetValue(value);
            }

            /// <summary>
            /// Sets a value (warning: overwrites existing values, use GetOrCreateValue to create values if needed)
            /// </summary>
            /// <typeparam name="T">Value type</typeparam>
            /// <param name="value">Value to assign</param>
            /// <returns></returns>
            public T SetValue<T>(T value)
            {
                string typeN = typeof(T).FullName;
                values[typeN] = value;
                return value;
            }

            /// <summary>
            /// Removes values
            /// </summary>
            /// <typeparam name="T">Value type</typeparam>
            /// <returns>Value object that was removed</returns>
            public T RemoveValue<T>()
            {
                string typeN = typeof(T).FullName;
                if (values.ContainsKey(typeN))
                {
                    T val = (T)values[typeN];
                    values.Remove(typeN);
                    return val;
                }
                return default;
            }

            /// <summary>
            /// Retrieves values by type
            /// </summary>
            /// <typeparam name="T">Value type</typeparam>
            /// <param name="name">Value key</param>
            /// <returns>Value instance</returns>
            public T GetValue<T>(string name)
            {
                string typeN = typeof(T).FullName + "_" + name;
                if (values.ContainsKey(typeN))
                    return (T) values[typeN];
                return default;
            }

            /// <summary>
            /// Retrieves values by type, and registers it if needed
            /// </summary>
            /// <typeparam name="T">Value type</typeparam>
            /// <param name="name">Value key</param>
            /// <param name="value">Value to assign</param>
            /// <returns>Value instance</returns>
            public T GetOrCreateValue<T>(string name, T value)
            {
                string typeN = typeof(T).FullName + "_" + name;
                if (values.ContainsKey(typeN))
                    return (T) values[typeN];
                return SetValue(name, value);
            }

            /// <summary>
            /// Sets a value (warning: overwrites existing values, use GetOrCreateValue to create values if needed)
            /// </summary>
            /// <typeparam name="T">Value type</typeparam>
            /// <param name="name">Value key</param>
            /// <param name="value">Value to assign</param>
            /// <returns></returns>
            public T SetValue<T>(string name, T value)
            {
                string typeN = typeof(T).FullName + "_" + name;
                values[typeN] = value;
                return value;
            }

            /// <summary>
            /// Removes values
            /// </summary>
            /// <typeparam name="T">Value type</typeparam>
            /// <param name="name">Value key</param>
            /// <returns>Value object that was removed</returns>
            public T RemoveValue<T>(string name)
            {
                string typeN = typeof(T).FullName + "_" + name;
                if (values.ContainsKey(typeN))
                {
                    T val = (T)values[typeN];
                    values.Remove(typeN);
                    return val;
                }
                return default;
            }
        }

        public class CoroutineExecutionContext
        {
            internal bool doBreak;
            internal Object returnValue;
            internal bool overrideReturn;
            internal bool doContinue;
            internal bool breakOrReturnCalled = false;
            internal CoroutineExecutionContextMemory memory;
            internal CoroutineExecutionContextMemory persistentMemory;

            public CoroutineExecutionContext(CoroutineExecutionContextMemory memory, CoroutineExecutionContextMemory persistentMemory)
            {
                this.memory = memory;
                this.persistentMemory = persistentMemory;
            }

            /// <summary>
            /// Retrieves the execution context memory for this instruction (persistent across Continue)
            /// </summary>
            public CoroutineExecutionContextMemory InstructionMemory
            {
                get
                {
                    return memory;
                }
            }

            /// <summary>
            /// Retrieves the execution context memory for the entire coroutine (persistent across all instructions)
            /// </summary>
            public CoroutineExecutionContextMemory PersistentMemory
            {
                get
                {
                    return persistentMemory;
                }
            }

            /// <summary>
            /// Tells the coroutine to keep this instruction for the next run and returns a specified value (make sure to use return Continue())
            /// </summary>
            /// <param name="val">Value to return</param>
            public Object Continue(Object val)
            {
                if (breakOrReturnCalled)
                    throw new System.InvalidOperationException("Unable to call Return() because the couroutine instruction was already ended, please avoid having multiple calls to break or return");
                returnValue = val;
                overrideReturn = true;
                breakOrReturnCalled = true;
                doContinue = true;
                return val;
            }

            /// <summary>
            /// Tells the coroutine to keep this instruction for the next run and returns a null value (make sure to use return Continue())
            /// </summary>
            public Object Continue()
            {
                if (breakOrReturnCalled)
                    throw new System.InvalidOperationException("Unable to call Return() because the couroutine instruction was already ended, please avoid having multiple calls to break or return");
                breakOrReturnCalled = true;
                doContinue = true;
                return null;
            }            

            /// <summary>
            /// Alters/sets the return value of the coroutine instruction (requivelant of yield return, make sure to use return Return(val))
            /// </summary>
            /// <param name="val">Value to return</param>
            public Object Return(Object val)
            {
                if (breakOrReturnCalled)
                    throw new System.InvalidOperationException("Unable to call Return() because the couroutine instruction was already ended, please avoid having multiple calls to break or return");
                returnValue = val;
                overrideReturn = true;
                breakOrReturnCalled = true;
                return val;
            }

            /// <summary>
            /// Returns a null instruction (make sure to use return Return())
            /// </summary>
            public Object Return()
            {
                if (breakOrReturnCalled)
                    throw new System.InvalidOperationException("Unable to call Return() because the couroutine instruction was already ended, please avoid having multiple calls to break or return");
                breakOrReturnCalled = true;
                return null;
            }

            /// <summary>
            /// Breaks the coroutine and stops execution (equivelant of yield break, make sure to use return Break())
            /// </summary>
            public void Break()
            {
                if (breakOrReturnCalled)
                    throw new System.InvalidOperationException("Unable to call Break() because the couroutine instruction was already ended, please avoid having multiple calls to break or return");
                doBreak = true;
                breakOrReturnCalled = true;
            }
        }

        public class CoroutineInst
        {
            public InstType type;
            public CoroutineExecutionDelegate ac;
            public IEnumerator cr;
            public System.Collections.IEnumerator crm;
            public CoroutineMultiInst[] aclst;
        }

        public class CoroutineMultiInst
        {
            public CoroutineInst insn;
            public bool finished;
        }
        
        public enum InstType
        {
            EXECUTE,
            EXECUTEMULTIPLE,
            EXECUTECOROUTINE,
            EXECUTEMANAGEDCOROUTINE,
            BREAK
        }

        private CoroutineInst[] lst;

        public FTCoroutine(nint pointer) : base(pointer)
        {
        }

        internal FTCoroutine(CoroutineBuilder builder) : base(ClassInjector.DerivedConstructorPointer<FTCoroutine>())
        {
            lst = builder.GetActions();
			ClassInjector.DerivedConstructorBody(this);
        }

        public class CoroutineBuilder
        {
            internal bool canDoMulti = true;
            private System.Collections.Generic.List<CoroutineInst> actions = new System.Collections.Generic.List<CoroutineInst>();

            /// <summary>
            /// Adds a break instruction
            /// </summary>
            public void Break()
            {
                actions.Add(new CoroutineInst()
                {
                    type = InstType.BREAK
                });
            }

            /// <summary>
            /// Adds a multi-execute instruction, supply a list of instructions that should run at the same frame update
            /// </summary>
            public void ExecuteMultiple(System.Action<CoroutineBuilder> call)
            {
                if (!canDoMulti)
                    throw new System.InvalidOperationException("Unable to call ExecuteMultiple in the current context");
                CoroutineBuilder sb = new CoroutineBuilder();
                sb.canDoMulti = false;
                call(sb);
                CoroutineInst[] insns = sb.GetActions();
                CoroutineMultiInst[] l = new CoroutineMultiInst[insns.Length];
                for (int i = 0; i < insns.Length; i++)
                {
                    l[i] = new CoroutineMultiInst()
                    {
                        insn = insns[i]
                    };
                }
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTEMULTIPLE,
                    aclst = l
                });
            }

            /// <summary>
            /// Adds a method execute instruction
            /// </summary>
            public void Execute(System.Action call)
            {
                Execute(t =>
                {
                    call();
                    return t.Return();
                });
            }

            /// <summary>
            /// Adds a method execute instruction with return value passed to the coroutine (return eg. new WaitForSeconds())
            /// </summary>
            public void Execute(System.Func<Object> call)
            {
                Execute(t =>
                {
                    return call();
                });
            }

            /// <summary>
            /// Adds a method execute instruction (supplying a CoroutineExecutionContext to the method call so the coroutine can be altered)
            /// </summary>
            public void Execute(System.Action<CoroutineExecutionContext> call)
            {
                Execute(ctx =>
                {
                    call(ctx);
                    return ctx.Return();
                });
            }

            /// <summary>
            /// Adds a method execute instruction (supplying a CoroutineExecutionContext to the method call so the coroutine can be altered)
            /// </summary>
            public void Execute(System.Func<CoroutineExecutionContext, Object> call)
            {
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTE,
                    ac = t => call(t)
                });
            }

            /// <summary>
            /// Adds a coroutine to execute
            /// </summary>
            /// <param name="call">Coroutine to execute</param>
            public void Execute(IEnumerator call)
            {
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTECOROUTINE,
                    cr = call
                });
            }

            /// <summary>
            /// Adds a coroutine to execute
            /// </summary>
            /// <param name="call">Coroutine to execute</param>
            public void Execute(System.Collections.IEnumerator call)
            {
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTEMANAGEDCOROUTINE,
                    crm = call
                });
            }

            /// <summary>
            /// Adds a coroutine to execute
            /// </summary>
            /// <param name="call">Coroutine to execute</param>
            public void Execute(FTCoroutine call)
            {
                Execute(FeralTweaksCoroutines.CastFT(call));
            }

            public CoroutineInst[] GetActions()
            {
                return actions.ToArray();
            }
        }

        private int _position = 0;
        private Object _current = null;
        private CoroutineExecutionContextMemory memPersistent = new CoroutineExecutionContextMemory();
        private CoroutineExecutionContextMemory memCurrentInsn = new CoroutineExecutionContextMemory();

        public Object Current => _current;

        private bool breakCalled = false;

        [HideFromIl2Cpp]
        private bool ExecInsn(CoroutineInst ac, ref int _position, ref Object _current, ref CoroutineExecutionContextMemory memCurrentInsn, bool moveMemory)
        {
            // Check type
            if (ac.type == InstType.BREAK)
            {
                _current = null;
                _position = lst.Length;
                breakCalled = true;
                return false;
            }
            else if (ac.type == InstType.EXECUTEMULTIPLE)
            {
                // Run underlying instructions
                bool allFinished = true;
                _current = null;
                foreach (CoroutineMultiInst insn in ac.aclst)
                {
                    if (!insn.finished)
                    {
                        allFinished = false;

                        // Run instruction   
                        int positionDummy = 0;
                        Object currentDummy = _current;
                        if (!ExecInsn(insn.insn, ref positionDummy, ref currentDummy, ref memCurrentInsn, false) || positionDummy != 0)
                        {
                            // Insn was finished
                            insn.finished = true;
                        }
                        else
                        {
                            // Executed successfully
                            if (currentDummy != null)
                                _current = currentDummy;
                        }
                        if (breakCalled)
                        {
                            // Handle break
                            _current = null;
                            _position = lst.Length;
                            breakCalled = true;
                            return false;
                        }
                    }
                }
                if (!allFinished)
                {
                    // Coroutine step
                    return true;
                }

                // Coroutine finished
                _position++;
                return MoveNext();
            }
            else if (ac.type == InstType.EXECUTECOROUTINE)
            {
                // Run underlying coroutine
                if (ac.cr.MoveNext())
                {
                    // Coroutine step
                    _current = ac.cr.Current;
                    return true;
                }

                // Coroutine finished
                _position++;
                _current = ac.cr.Current;
                return MoveNext();
            }
            else if (ac.type == InstType.EXECUTEMANAGEDCOROUTINE)
            {
                // Run underlying coroutine
                if (ac.crm.MoveNext())
                {
                    // Coroutine step
                    object cVal = ac.crm.Current;
                    Object castedVal;
                    if (cVal == null)
                        castedVal = null;
                    else if (cVal is Il2CppObjectBase)
                        castedVal = ((Il2CppObjectBase)cVal).Cast<Object>();
                    else
                        throw new System.ArgumentException("Managed coroutine returned non-null non-il2cpp object, unable to cast");
                    _current = castedVal;
                    return true;
                }

                // Coroutine finished
                _position++;
                object cVal2 = ac.crm.Current;
                Object castedVal2;
                if (cVal2 == null)
                    castedVal2 = null;
                else if (cVal2 is Il2CppObjectBase)
                    castedVal2 = ((Il2CppObjectBase)cVal2).Cast<Object>();
                else
                    throw new System.ArgumentException("Managed coroutine returned non-null non-il2cpp object, unable to cast");
                _current = castedVal2;
                return MoveNext();
            }

            // Execute
            CoroutineExecutionContext ctx = new CoroutineExecutionContext(memCurrentInsn, memPersistent);
            _current = ac.ac(ctx);

            // Check override
            if (ctx.overrideReturn)
                _current = ctx.returnValue;
            if (ctx.doBreak)
            {
                _position = lst.Length;
                return true;
            }
            if (!ctx.doContinue)
            {
                _position++;
                if (moveMemory)
                    memCurrentInsn = new CoroutineExecutionContextMemory();
            }
            return true;
        }

        public bool MoveNext()
        {
            if (_position >= lst.Length)
                return false;

            // Run routine and return
            CoroutineInst ac = lst[_position];
            return ExecInsn(ac, ref _position, ref _current, ref memCurrentInsn, true);
        }

        public void Reset()
        {
            _position = 0;
            _current = null;
            memCurrentInsn = new CoroutineExecutionContextMemory();
            memPersistent = new CoroutineExecutionContextMemory();
            breakCalled = false;
        }
    }
}