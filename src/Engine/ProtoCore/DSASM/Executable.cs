using System;
using System.Collections.Generic;
using System.Linq;
using ProtoCore.AssociativeGraph;
using ProtoCore.Lang;
using ProtoFFI;
namespace ProtoCore.DSASM
{
    /// <summary>
    /// InstructionStream holds the executable dsasm code and relevant information
    /// </summary>
    /// 
    public class InstructionStream
    {
        public ProtoCore.Language language { get; set; }
        public int entrypoint { get; set; }
        public List<Instruction> instrList { get; set; }
        public ProtoCore.AssociativeGraph.DependencyGraph dependencyGraph { get; set; }
        public List<ProtoCore.AssociativeGraph.UpdateNodeRef> xUpdateList { get; set; }

        public InstructionStream(ProtoCore.Language langId, ProtoCore.Core core)
        {
            language = langId;
            entrypoint = Constants.kInvalidIndex;
            instrList = new List<Instruction>();
            dependencyGraph = new ProtoCore.AssociativeGraph.DependencyGraph(core);
            xUpdateList = new List<AssociativeGraph.UpdateNodeRef>();
        }
    }

    /// <summary>
    /// Executable holds the body of code that will be executed along with associated
    /// meta-information
    /// </summary>
    /// 
    public class Executable
    {
        /// <summary>
        /// RuntimeData is set in the executable to isolate data passed to the runtime VM
        /// The RuntimeData will eventually be integrated completely into executable,
        /// this means moving RuntimeData properties to Executable and deprecating the RuntimeData object
        /// </summary>

        public ProtoCore.DSASM.ClassTable classTable { get; set; }
        public ProtoCore.DSASM.ProcedureTable[] procedureTable { get; set; }
        public ProtoCore.DSASM.SymbolTable[] runtimeSymbols { get; set; }
        public TypeSystem TypeSystem { get; set; }

        public List<CodeBlock> CodeBlocks { get; set; }

        internal SortedDictionary<int, CodeBlock> CompleteCodeBlockDict { get; set; }

        public InstructionStream[] instrStreamList { get; set; } 
        public InstructionStream iStreamCanvas { get; set; }

        public DebugServices.EventSink EventSink = new DebugServices.ConsoleEventSink();

        
#region COMPILER_GENERATED_READ_ONLY
        public FunctionTable FunctionTable { get; set; }
        /// <summary>
        /// This is a mapping of the current guid and number of callsites that appear within that guid.
        /// Language only execution contains only 1 guid for the entire program.
        /// Execution within a visual programming host means 1 guid per node, where 1 node contains a set of DS code.
        /// Each of the callsite instances are mapped to a guid and an instance count.
        /// </summary>
        public DynamicVariableTable DynamicVarTable { get; set; }
        public DynamicFunctionTable DynamicFuncTable { get; set; }
        public FunctionPointerTable FuncPointerTable { get; set; }
        internal ContextDataManager ContextDataMngr { get; set; }
        public IDictionary<string, object> Configurations { get; set; }
        public Dictionary<ulong, ulong> CodeToLocation { get; set; }
        public string CurrentDSFileName { get; set; }
       
#region RUNTIME_GENERATED
        // RUNTIME_GENERATED are properties generated by the runtime VM and are read-write

        /// <summary>
        ///  These are the list of symbols updated by the VM after an execution cycle
        /// </summary>
        public HashSet<SymbolNode> UpdatedSymbols { get; private set; }
        public GraphNode ExecutingGraphnode { get; set; }
#endregion
#endregion

        public Executable()
        {
            Reset();
        }

        public void Reset()
        {
            runtimeSymbols = null;
            procedureTable = null;
            classTable = null;
            instrStreamList = null;
            iStreamCanvas = null;
            CodeBlocks = null;
            CompleteCodeBlockDict = null;
            ContextDataMngr = null;
            CodeToLocation = null;
            CurrentDSFileName = string.Empty;
            UpdatedSymbols = new HashSet<SymbolNode>();
        }
    }

    public enum CodeBlockType
    {
        Language,
        Construct,
        Function,
    }

    public class CodeBlock
    {
        public Guid guid { get; set; }
        public CodeBlockType blockType { get; set; }

        public CodeBlock parent { get; set; }
        public List<CodeBlock> children { get; set; }

        public Language language { get; private set; }
        public int codeBlockId { get; private set; }

        public SymbolTable symbolTable { get; set; }
        public ProcedureTable procedureTable { get; private set; }
        public List<AttributeEntry> Attributes { get; set; }
        public InstructionStream instrStream { get; set; }

        public DebugServices.EventSink EventSink = new DebugServices.ConsoleEventSink();

        public bool isBreakable { get; set; }

        /// <summary>
        /// A CodeBlock represents a body of DS code
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="type"></param>
        /// <param name="langId"></param>
        /// <param name="cbID"></param>
        /// <param name="symbols"></param>
        /// <param name="procTable"></param>
        /// <param name="isBreakableBlock"></param>
        /// <param name="core"></param>
        public CodeBlock(Guid guid, CodeBlockType type, ProtoCore.Language langId, int cbID, SymbolTable symbols, ProcedureTable procTable, bool isBreakableBlock = false, ProtoCore.Core core = null)
        {
            this.guid = guid;
            blockType = type;

            parent = null;
            children = new List<CodeBlock>();

            language = langId;
            instrStream = new InstructionStream(langId, core);

            symbolTable = symbols;
            procedureTable = procTable;

            isBreakable = isBreakableBlock;
            codeBlockId = core.GetRuntimeTableSize();
            core.CompleteCodeBlockDict.Add(codeBlockId, this);

            symbols.RuntimeIndex = codeBlockId;

            if (core.ProcNode != null)
            {
                core.ProcNode.ChildCodeBlocks.Add(codeBlockId);
            }
        }

        public bool IsMyAncestorBlock(int blockId)
        {
            CodeBlock ancestor = this.parent;
            while (ancestor != null)
            {
                if (ancestor.codeBlockId == blockId)
                {
                    return true;
                }
                else
                {
                    ancestor = ancestor.parent;
                }
            }
            return false;
        }
    }
}
