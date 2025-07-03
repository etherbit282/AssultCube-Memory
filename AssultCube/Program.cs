using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, int size, out int bytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

    const int PROCESS_VM_READ = 0x0010;
    const int PROCESS_VM_OPERATION = 0x0008;
    const int PROCESS_VM_WRITE = 0x0020;

    static IntPtr processHandle;
    static IntPtr healthAddress;
    static IntPtr armourAddress;
    static IntPtr nameAddress;
    static IntPtr pistolAddress;
    static IntPtr rifleAddress;
    static IntPtr ammoAddress;

    static void Main()
    {
        int local_player_offset = 0x0017E0A8;
        int health_offset = 0xEC;
        int armour_offset = 0xF0;
        int name_offset = 0x205;
        int ammo_pistol_offset = 0x12C;
        int ammo_rifle_offset = 0x140;

        var processes = Process.GetProcessesByName("ac_client");
        if (processes.Length == 0)
        {
            Console.WriteLine("Process not found.");
            return;
        }

        var process = processes[0];
        processHandle = OpenProcess(PROCESS_VM_READ | PROCESS_VM_OPERATION | PROCESS_VM_WRITE, false, process.Id);
        if (processHandle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to open process. Error: " + Marshal.GetLastWin32Error());
            return;
        }

        IntPtr baseAddress = process.MainModule.BaseAddress;
        IntPtr localPlayerPtrAddress = IntPtr.Add(baseAddress, local_player_offset);

        byte[] buffer = new byte[IntPtr.Size];
        if (!ReadProcessMemory(processHandle, localPlayerPtrAddress, buffer, buffer.Length, out _))
        {
            Console.WriteLine("Failed to read local player pointer.");
            return;
        }

        IntPtr localPlayerPtr = (IntPtr)(BitConverter.ToInt32(buffer, 0));

        healthAddress = IntPtr.Add(localPlayerPtr, health_offset);
        armourAddress = IntPtr.Add(localPlayerPtr, armour_offset);
        nameAddress = IntPtr.Add(localPlayerPtr, name_offset);
        pistolAddress = IntPtr.Add(localPlayerPtr, ammo_pistol_offset);
        rifleAddress = IntPtr.Add(localPlayerPtr, ammo_rifle_offset);

        byte[] healthBuffer = new byte[4];
        byte[] armourBuffer = new byte[4];
        byte[] nameBuffer = new byte[32];
        byte[] pistolBuffer = new byte[4];
        ReadInfo();
        ChangeHealth(2872);
        ChangeAmmo(1000);
        ChangeArmour(1000);
        ChangeName("Diet");
        ReadInfo();
    }

    static void ChangeHealth(int newHealth)
    {
        byte[] bytes_health = BitConverter.GetBytes(newHealth);
        bool result = WriteProcessMemory(processHandle, healthAddress, bytes_health, bytes_health.Length, out int written);
        Console.WriteLine("New health written: " + result + ", value: " + newHealth);
    }

    static void ChangeAmmo(int newAmmo)
    {
        byte[] bytes_ammo = BitConverter.GetBytes(newAmmo);
        bool result = WriteProcessMemory(processHandle, ammoAddress, bytes_ammo, bytes_ammo.Length, out int written);
        Console.WriteLine("New ammo written: " + result + ", value: " + newAmmo);
    }

    static void ChangeArmour(int newArmour)
    {
        byte[] bytes_armour = BitConverter.GetBytes(newArmour);
        bool result = WriteProcessMemory(processHandle, armourAddress, bytes_armour, bytes_armour.Length, out int written);
        Console.WriteLine("New armour written: " + result + ", value: " + newArmour);
    }

    static void ChangeName(string newName)
    {
        byte[] bytes_name = new byte[32];
        byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(newName);

        Array.Copy(nameBytes, bytes_name, Math.Min(nameBytes.Length, bytes_name.Length));

        bool result = WriteProcessMemory(processHandle, nameAddress, bytes_name, bytes_name.Length, out int written);
        Console.WriteLine("New name written: " + result + ", value: " + newName);
    }
    
    static void ReadInfo()
    {
        byte[] healthBuffer = new byte[4];
        byte[] armourBuffer = new byte[4];
        byte[] nameBuffer = new byte[32];
        byte[] pistolBuffer = new byte[4];
        ReadProcessMemory(processHandle, armourAddress, armourBuffer, armourBuffer.Length, out _);
        ReadProcessMemory(processHandle, healthAddress, healthBuffer, healthBuffer.Length, out _);
        ReadProcessMemory(processHandle, nameAddress, nameBuffer, nameBuffer.Length, out _);
        ReadProcessMemory(processHandle, pistolAddress, pistolBuffer, pistolBuffer.Length, out _);
        int armour = BitConverter.ToInt32(armourBuffer, 0);
        int health = BitConverter.ToInt32(healthBuffer, 0);
        string name = System.Text.Encoding.ASCII.GetString(nameBuffer).Split('\0')[0];
        int pistolammo = BitConverter.ToInt32(pistolBuffer, 0);
        Console.WriteLine("Armour: " + armour);
        Console.WriteLine("Health: " + health);
        Console.WriteLine("Player Name: " + name);
        Console.WriteLine("Ammo Pistol: " + pistolammo);
    }
}