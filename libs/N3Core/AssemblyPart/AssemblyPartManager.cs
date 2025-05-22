using System.Reflection;
using System.Runtime.Loader;
using NLog;

namespace N3Core;

public sealed class AssemblyPartManager : Singleton<AssemblyPartManager>
{
    private static readonly SLogger Logger = nameof(AssemblyPartManager);
    private static readonly object LockObj = new object();

    private AssemblyLoadContext? _assemblyLoadContext;
    private readonly List<IAssemblyPostProcess> _assemblyPostProcesses = new();
    private static readonly HashSet<Assembly> Assemblies = new();
    private static readonly Dictionary<string, Assembly> HotfixAssemblies = new();

    private FileSystemWatcher? _fileWatcher;
    private int _reloadDelayCounter = 0;
    private int _reloadDelaySeconds = 5;

    private SynchronizationContext? _synchronizationContext;

    /// <summary>
    /// 设置Watcher热更dll变动时Reload的延迟秒数(默认5s)
    ///     最小5s
    /// </summary>
    public int ReloadDelaySeconds
    {
        get => _reloadDelaySeconds;
        set => _reloadDelaySeconds = Math.Max(value, 5);
    }

    private AssemblyPartManager()
    {
        this.AddPostProcess(TypeManager.Ins);
    }

    /// <summary>
    /// 添加一个非热更程序集
    /// </summary>
    /// <param name="assembly"></param>
    public AssemblyPartManager AddPart(Assembly assembly)
    {
        if (!Assemblies.Add(assembly))
            throw new Exception("请不要重复添加程序集.");
        return this;
    }

    private (string, string) GetDllAndPdbPath(string assemblyName)
    {
        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        string dllFilePath = Path.Combine(currentDirectory, $"{assemblyName}.dll");
        string pdbFilePath = Path.Combine(currentDirectory, $"{assemblyName}.pdb");
        return (dllFilePath, pdbFilePath);
    }

    /// <summary>
    /// 添加一个可热更的程序集
    /// </summary>
    /// <param name="assemblyName"></param>
    public AssemblyPartManager AddHotfixPart(string assemblyName)
    {
        (string dllFilePath, string pdbFilePath) = GetDllAndPdbPath(assemblyName);

        if (!File.Exists(dllFilePath))
            throw new FileNotFoundException(dllFilePath);

        if (!File.Exists(pdbFilePath))
            throw new FileNotFoundException(dllFilePath);

        // 添加时就先载入
        _assemblyLoadContext ??= new AssemblyLoadContext("hotfix", true);

        using var dll = File.OpenRead(dllFilePath);
        using var pdb = File.OpenRead(pdbFilePath);
        Assembly assembly = _assemblyLoadContext.LoadFromStream(dll, pdb);
        if (!HotfixAssemblies.TryAdd(assemblyName, assembly))
            throw new Exception("请不要重复添加热更程序集.");

        AssemblyVersionAttribute? attr = assembly.GetCustomAttribute<AssemblyVersionAttribute>();
        Logger.Info($"add hotfix assembly: {assemblyName}");
        return this;
    }

    /// <summary>
    /// 配置一个程序集后处理
    /// </summary>
    public AssemblyPartManager AddPostProcess(IAssemblyPostProcess postProcess)
    {
        _assemblyPostProcesses.Add(postProcess);
        return this;
    }

    public void Load()
    {
        lock (LockObj)
        {
            Logger.Info($"Assembly count: {Assemblies.Count} {HotfixAssemblies.Count}");
            Load(false);
        }
    }

    /// <summary>
    /// 启用热更dll文件监听(热更dll变动后，自动reload)
    /// </summary>
    /// <returns></returns>
    public AssemblyPartManager EnableWatch(bool catchSynchronizationContext = true)
    {
        if (_fileWatcher != null)
            return this;

        _synchronizationContext = SynchronizationContext.Current;

        _fileWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
        _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        _fileWatcher.EnableRaisingEvents = true;
        _fileWatcher.Changed += OnFileChanged;
        Logger.Info($"Start watcher: {_fileWatcher.Path} {_fileWatcher.Filter}");
        return this;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var current = Interlocked.Increment(ref _reloadDelayCounter);
        Task.Delay(1000 * _reloadDelaySeconds).ContinueWith(_ =>
        {
            string? changedName = Path.GetFileNameWithoutExtension(e.Name);
            if (string.IsNullOrEmpty(changedName))
                return;

            if (!HotfixAssemblies.ContainsKey(changedName))
                return;

            Logger.Info($"Watcher file changed: {e.Name}");

            if (current != _reloadDelayCounter)
                return;

            if (_synchronizationContext != null)
            {
                _synchronizationContext.Post(_ => { ReloadHotfix(); }, null);
            }
            else
            {
                ReloadHotfix();
            }
        });
    }

    private void ReloadHotfix()
    {
        lock (LockObj)
        {
            if (HotfixAssemblies is { Count: 0 }) return;

            string[] names = HotfixAssemblies.Keys.ToArray();
            AssemblyLoadContext assemblyLoadContext = new AssemblyLoadContext("hotfix", true);

            foreach (var fileName in names)
            {
                (string dllFilePath, string pdbFilePath) = GetDllAndPdbPath(fileName);
                if (File.Exists(dllFilePath) && File.Exists(pdbFilePath))
                {
                    using var dll = File.OpenRead(dllFilePath);
                    using var pdb = File.OpenRead(pdbFilePath);
                    Assembly assembly = assemblyLoadContext.LoadFromStream(dll, pdb);
                    HotfixAssemblies[fileName] = assembly;
                    Logger.Info($"Reload hotfix: {fileName}");
                }
                else
                {
                    Logger.Warn($"Reload hotfix: {fileName} dll or pdb not found!");
                }
            }

            _assemblyLoadContext?.Unload();
            _assemblyLoadContext = assemblyLoadContext;
            Load(true);
        }
    }

    private void Load(bool onlyHotfix)
    {
        foreach (var postProcess in _assemblyPostProcesses)
        {
            postProcess.Begin();
        }

        foreach (var assembly in Assemblies)
        {
            Process(assembly, false);
        }

        foreach (var assembly in HotfixAssemblies.Values)
        {
            Process(assembly, true);
        }

        void Process(Assembly assembly, bool isHotfix)
        {
            ServerTypeAttribute? attr = assembly.GetCustomAttribute<ServerTypeAttribute>();
            if (isHotfix && attr is null)
                SLog.Warn($"程序集没有标记ServerTypeAttribute: {assembly.FullName}");

            foreach (var type in assembly.GetTypes())
            {
                foreach (var postProcess in _assemblyPostProcesses)
                {
                    postProcess.Process(attr?.ServerType ?? 0, type, isHotfix);
                }
            }
        }

        foreach (var postProcess in _assemblyPostProcesses)
        {
            postProcess.End();
        }
    }
}