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
            public CoroutineResultReference refer;
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

            private CoroutineResultReference<T> GenRef<T>()
            {
                CoroutineResultReference<T> refer = new CoroutineResultReference<T>();
                refer._memory = new CoroutineExecutionContextMemory();
                return refer;
            }

            /// <summary>
            /// Adds a break instruction
            /// </summary>
            public CoroutineResultReference<Object> Break()
            {
                CoroutineResultReference<Object> refer = GenRef<Object>();
                actions.Add(new CoroutineInst()
                {
                    type = InstType.BREAK,
                    refer = refer
                });
                return refer;
            }

            /// <summary>
            /// Adds a multi-execute instruction, supply a list of instructions that should run at the same frame update
            /// </summary>
            public CoroutineResultReference<Object> ExecuteMultiple(System.Action<CoroutineBuilder> call)
            {
                CoroutineResultReference<Object> refer = GenRef<Object>();
                if (!canDoMulti)
                    throw new System.InvalidOperationException("Unable to call ExecuteMultiple in the current context");
                CoroutineBuilder sb = new CoroutineBuilder();
                sb.canDoMulti = false;
                call(sb);
                CoroutineInst[] insns = sb.GetActions();
                CoroutineMultiInst[] l = new CoroutineMultiInst[insns.Length];
                for (int i = 0; i < insns.Length; i++)
                {
                    // Share memory
                    insns[i].refer._memory = refer._memory;
                    l[i] = new CoroutineMultiInst()
                    {
                        insn = insns[i]
                    };
                }
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTEMULTIPLE,
                    aclst = l,
                    refer = refer
                });
                return refer;
            }

            /// <summary>
            /// Adds a FT action execute instruction
            /// </summary>
            public CoroutineResultReference<FeralTweaksAction<object>> Execute(System.Func<FeralTweaksAction<object>> actionBuilder)
            {
                return Execute<object>(actionBuilder);
            }

            /// <summary>
            /// Adds a FT action execute instruction
            /// </summary>
            public CoroutineResultReference<FeralTweaksAction<T>> Execute<T>(System.Func<FeralTweaksAction<T>> actionBuilder)
            {
                CoroutineResultReference<FeralTweaksAction<T>> refer = GenRef<FeralTweaksAction<T>>();
                refer._alwaysReturn = true;
                refer._selfAssignValue = true;
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTE,
                    ac = ctx =>
                    {
                        FeralTweaksAction<T> ac = (FeralTweaksAction<T>)refer._value;
                        if (ac == null)
                        {
                            ac = actionBuilder();
                            refer._value = ac;
                        }

                        // Wait
                        if (!ac.HasCompleted)
                            return ctx.Continue();

                        // Return
                        return ctx.Return();
                    },
                    refer = refer
                });
                return refer;
            }

            /// <summary>
            /// Adds a method execute instruction
            /// </summary>
            public CoroutineResultReference<Object> Execute(System.Action call)
            {
                return Execute(t =>
                {
                    call();
                    return t.Return();
                });
            }

            /// <summary>
            /// Adds a method execute instruction with return value passed to the coroutine (return eg. new WaitForSeconds())
            /// </summary>
            public CoroutineResultReference<T> Execute<T>(System.Func<T> call) where T : Object
            {
                return Execute<T>(t =>
                {
                    return call();
                });
            }

            /// <summary>
            /// Adds a method execute instruction (supplying a CoroutineExecutionContext to the method call so the coroutine can be altered)
            /// </summary>
            public CoroutineResultReference<Object> Execute(System.Action<CoroutineExecutionContext> call)
            {
                return Execute(ctx =>
                {
                    call(ctx);
                    return ctx.Return();
                });
            }

            /// <summary>
            /// Adds a method execute instruction (supplying a CoroutineExecutionContext to the method call so the coroutine can be altered)
            /// </summary>
            public CoroutineResultReference<T> Execute<T>(System.Func<CoroutineExecutionContext, T> call) where T : Object
            {
                CoroutineResultReference<T> refer = GenRef<T>();
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTE,
                    ac = t => call(t),
                    refer = refer
                });
                return refer;
            }

            /// <summary>
            /// Adds a coroutine to execute
            /// </summary>
            /// <param name="call">Coroutine to execute</param>
            public CoroutineResultReference<IEnumerator> Execute(IEnumerator call)
            {
                CoroutineResultReference<IEnumerator> refer = GenRef<IEnumerator>();
                refer._selfAssignValue = true;
                refer._retainValue = true;
                refer._value = call;
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTECOROUTINE,
                    cr = call,
                    refer = refer
                });
                return refer;
            }

            /// <summary>
            /// Adds a coroutine to execute
            /// </summary>
            /// <param name="call">Coroutine to execute</param>
            public CoroutineResultReference<System.Collections.IEnumerator> Execute(System.Collections.IEnumerator call)
            {
                CoroutineResultReference<System.Collections.IEnumerator> refer = GenRef<System.Collections.IEnumerator>();
                refer._selfAssignValue = true;
                refer._retainValue = true;
                refer._value = call;
                actions.Add(new CoroutineInst()
                {
                    type = InstType.EXECUTEMANAGEDCOROUTINE,
                    crm = call,
                    refer = refer
                });
                return refer;
            }

            /// <summary>
            /// Adds a coroutine to execute
            /// </summary>
            /// <param name="call">Coroutine to execute</param>
            public CoroutineResultReference<IEnumerator> Execute(FTCoroutine call)
            {
                return Execute(FeralTweaksCoroutines.CastFT(call));
            }

            public CoroutineInst[] GetActions()
            {
                return actions.ToArray();
            }
        }

        private int _position = 0;
        private Object _current = null;
        private CoroutineExecutionContextMemory memPersistent = new CoroutineExecutionContextMemory();

        public Object Current => _current;

        private bool breakCalled = false;

        [HideFromIl2Cpp]
        private bool ExecInsn(CoroutineInst ac, ref int _position, ref Object _current, bool moveMemory)
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
                        if (!ExecInsn(insn.insn, ref positionDummy, ref currentDummy, false) || positionDummy != 0)
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
            CoroutineExecutionContext ctx = new CoroutineExecutionContext(ac.refer._memory, memPersistent);
            _current = ac.ac(ctx);

            // Assign value
            if (!ac.refer._selfAssignValue)
                ac.refer._value = _current;

            // Check override
            if (ctx.overrideReturn)
                _current = ctx.returnValue;

            // Break if needed
            if (ctx.doBreak)
            {
                _position = lst.Length;
                ac.refer._completed = true;
                return true;
            }

            // Move on
            if (!ctx.doContinue)
            {
                _position++;
                ac.refer._completed = true;
            }
            return true;
        }

        public bool MoveNext()
        {
            if (_position >= lst.Length)
                return false;

            // Run routine and return
            CoroutineInst ac = lst[_position];
            return ExecInsn(ac, ref _position, ref _current, true);
        }

        public void Reset()
        {
            // Reset routine
            _position = 0;
            _current = null;
            memPersistent = new CoroutineExecutionContextMemory();
            breakCalled = false;

            // Reset nodes
            foreach (CoroutineInst inst in lst)
                ResetInst(inst);
        }

        [HideFromIl2Cpp]
        private void ResetInst(CoroutineInst inst)
        {
            // Reset instruction reference object
            inst.refer._completed = false;
            if (!inst.refer._retainValue)
                inst.refer._value = null;
            inst.refer._memory = new CoroutineExecutionContextMemory();

            // Reset nodes
            if (inst.aclst != null)
            {
                foreach (CoroutineMultiInst inst2 in inst.aclst)
                {
                    ResetInst(inst2.insn);
                    inst2.insn.refer._memory = inst.refer._memory;
                }
            }
        }
    }

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
                return (T)values[typeN];
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
                return (T)values[typeN];
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
                return (T)values[typeN];
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
                return (T)values[typeN];
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

    public abstract class CoroutineResultReference
    {
        internal bool _alwaysReturn;
        internal bool _selfAssignValue;
        internal bool _retainValue;
        internal object _value;
        internal CoroutineExecutionContextMemory _memory;
        internal bool _completed;

        /// <summary>
        /// Checks if the coroutine instruction had finished
        /// </summary>
        public abstract bool InstructionCompleted { get; }

        /// <summary>
        /// Retrieves the instruction memory of the instruction in question
        /// </summary>
        public abstract CoroutineExecutionContextMemory InstructionMemory { get; }
    }

    public class CoroutineResultReference<T> : CoroutineResultReference
    {
        /// <summary>
        /// Checks if the coroutine instruction had finished
        /// </summary>
        public override bool InstructionCompleted
        {
            get
            {
                return _completed;
            }
        }

        /// <summary>
        /// Retrieves the return value of the coroutine instruction
        /// </summary>
        public T ReturnValue
        {
            get
            {
                if ((!InstructionCompleted && !_alwaysReturn) || _value == null)
                    return default(T);
                return (T)_value;
            }
        }

        /// <summary>
        /// Retrieves the instruction memory of the instruction in question
        /// </summary>
        public override CoroutineExecutionContextMemory InstructionMemory
        {
            get
            {
                return _memory;
            }
        }
    }
}