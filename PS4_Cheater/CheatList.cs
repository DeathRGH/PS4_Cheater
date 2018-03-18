﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PS4_Cheater
{
    public enum CheatType
    {
        DATA_TYPE,
        HEX_TYPE,
        NONE_TYPE,
    }

    public class Cheat
    {
        public string Address { get; set; }

        public ValueType Type { get; set; }

        public virtual string Value { get; set; }

        public CheatType CheatType { get; set; }

        public string Description { get; set; }

        public bool Lock { get; set; }

        public bool AllowLock { get; set; }

        public ProcessManager processManager { get; set; }

        public virtual bool Load(string[] elements, ProcessManager processManager)
        {
            return false;
        }

        public Cheat(ProcessManager processManager)
        {
            this.processManager = processManager;
        }

        public static CheatType GetCheatTypeByValueType(ValueType Type)
        {
            switch (Type)
            {
                case ValueType.BYTE_TYPE:
                case ValueType.USHORT_TYPE:
                case ValueType.UINT_TYPE:
                case ValueType.ULONG_TYPE:
                case ValueType.FLOAT_TYPE:
                case ValueType.DOUBLE_TYPE:
                    return CheatType.DATA_TYPE;
                case ValueType.HEX_TYPE:
                    return CheatType.HEX_TYPE;
                default:
                    throw new ArgumentException("Unkown value type.");
            }
        }

        public virtual void Refresh()
        {

        }

        public virtual bool Legal()
        {
            return false;
        }

        public virtual string Save()
        {
            return "";
        }
    }

    public class DataCheat : Cheat
    {
        private const int CHEAT_CODE_DATA_TYPE_SECTION_ID = 1;
        private const int CHEAT_CODE_DATA_TYPE_ADDRESS_OFFSET = 2;
        private const int CHEAT_CODE_DATA_TYPE_VALUE_TYPE = 3;
        private const int CHEAT_CODE_DATA_TYPE_VALUE = 4;
        private const int CHEAT_CODE_DATA_TYPE_FLAG = 5;
        private const int CHEAT_CODE_DATA_TYPE_DESCRIPTION = 6;

        private const int CHEAT_CODE_DATA_TYPE_ELEMENT_COUNT = CHEAT_CODE_DATA_TYPE_DESCRIPTION + 1;

        private string value_;
        public override string Value {
            get {
                return value_;
            }
            set {
                value_ = value;
                MemoryHelper memoryHelper = new MemoryHelper();
                memoryHelper.InitMemoryHandler(Type, CompareType.NONE, false);
                memoryHelper.SetBytesByType(ulong.Parse(Address, NumberStyles.HexNumber), memoryHelper.StringToBytes(value_));
            }
        }

        public int SectionID { get; set; }
        public DataCheat(ProcessManager processManager, string address, int sectionID, string value, bool lock_, ValueType type, string description) :
            base(processManager)
        {
            CheatType = CheatType.DATA_TYPE;
            AllowLock = true;
            this.Address = address;
            this.SectionID = sectionID;
            this.Lock = lock_;
            this.Type = type;
            this.Description = description;
            this.Value = value;
        }

        public DataCheat(ProcessManager processManager) :
            base(processManager)
        {
            CheatType = CheatType.DATA_TYPE;
            AllowLock = true;
        }

        public override bool Load(string[] elements, ProcessManager processManager)
        {
            if (elements.Length < CHEAT_CODE_DATA_TYPE_ELEMENT_COUNT)
            {
                return false;
            }

            SectionID = int.Parse(elements[CHEAT_CODE_DATA_TYPE_SECTION_ID]);
            if (SectionID >= processManager.MappedSectionList.Count || SectionID < 0)
            {
                return false;
            }

            Address = (ulong.Parse(elements[CHEAT_CODE_DATA_TYPE_ADDRESS_OFFSET], NumberStyles.HexNumber) +
                processManager.MappedSectionList[SectionID].Start).ToString("X");

            Type = MemoryHelper.GetValueTypeByString(elements[CHEAT_CODE_DATA_TYPE_VALUE_TYPE]);

            ulong flag = ulong.Parse(elements[CHEAT_CODE_DATA_TYPE_FLAG], NumberStyles.HexNumber);

            Lock = false;
            if ((flag & CONSTANT.SAVE_FLAG_LOCK) == CONSTANT.SAVE_FLAG_LOCK)
            {
                Lock = true;
            }

            Description = elements[CHEAT_CODE_DATA_TYPE_DESCRIPTION];

            value_ = elements[CHEAT_CODE_DATA_TYPE_VALUE];

            return true;
        }

        public override string Save()
        {
            ulong addressDec = ulong.Parse(Address, NumberStyles.HexNumber);
            MappedSection mappedSection = processManager.GetMappedSection(addressDec);
            string save_buf = "";
            save_buf += "data|";
            save_buf += SectionID + "|";
            save_buf += String.Format("{0:X}", addressDec - mappedSection.Start) + "|";
            save_buf += MemoryHelper.GetStringOfValueType(Type) + "|";
            save_buf += Value + "|";
            save_buf += Lock ? "1" : "0" + "|";
            save_buf += Description + "|\n";
            return save_buf;
        }

        public override void Refresh()
        {
            MemoryHelper memoryHelper = new MemoryHelper();
            memoryHelper.InitMemoryHandler(Type, CompareType.NONE, false);

            memoryHelper.SetBytesByType(ulong.Parse(Address, NumberStyles.HexNumber), memoryHelper.StringToBytes(value_));
            value_ = memoryHelper.BytesToString(memoryHelper.GetBytesByType(ulong.Parse(Address, NumberStyles.HexNumber)));
        }
    }

    public class HexCheat : Cheat
    {
        private const int CHEAT_CODE_HEX_TYPE_SECTION_ID = 1;
        private const int CHEAT_CODE_HEX_TYPE_ADDRESS_OFFSET = 2;
        private const int CHEAT_CODE_HEX_TYPE_VALUE_TYPE = 3;
        private const int CHEAT_CODE_HEX_TYPE_LEN = 4;
        private const int CHEAT_CODE_HEX_TYPE_VALUE = 5;
        private const int CHEAT_CODE_HEX_TYPE_FLAG = 6;
        private const int CHEAT_CODE_HEX_TYPE_DESCRIPTION = 7;

        private const int CHEAT_CODE_HEX_TYPE_ELEMENT_COUNT = CHEAT_CODE_HEX_TYPE_DESCRIPTION + 1;

        public int SectionID { get; set; }

        private string value_;
        public override string Value
        {
            get
            {
                return value_;
            }
            set
            {
                value_ = value;
                MemoryHelper.WriteMemory(ulong.Parse(Address, NumberStyles.HexNumber), MemoryHelper.string_to_hex(value_));
            }
        }

        public HexCheat(ProcessManager processManager, string address, int sectionID,
            string value, bool lock_, ValueType type, string description):
            base(processManager)
        {
            this.Address = address;
            this.SectionID = sectionID;
            this.Type = type;
            this.Description = description;
            CheatType = CheatType.HEX_TYPE;
            AllowLock = false;
            this.Value = value;
        }

        public HexCheat(ProcessManager processManager):
                        base(processManager)
        {
            CheatType = CheatType.HEX_TYPE;
            AllowLock = false;
        }

        public override bool Load(string[] elements, ProcessManager processManager)
        {
            if (elements.Length < CHEAT_CODE_HEX_TYPE_ELEMENT_COUNT)
            {
                return false;
            }

            int sectionID = int.Parse(elements[CHEAT_CODE_HEX_TYPE_SECTION_ID]);
            if (sectionID >= processManager.MappedSectionList.Count || sectionID < 0)
            {
                return false;
            }

            Address = (ulong.Parse(elements[CHEAT_CODE_HEX_TYPE_ADDRESS_OFFSET], NumberStyles.HexNumber) +
                processManager.MappedSectionList[sectionID].Start).ToString("X2");

            Type = MemoryHelper.GetValueTypeByString(elements[CHEAT_CODE_HEX_TYPE_VALUE_TYPE]);
            value_ = elements[CHEAT_CODE_HEX_TYPE_VALUE];

            ulong flag = ulong.Parse(elements[CHEAT_CODE_HEX_TYPE_FLAG], NumberStyles.HexNumber);

            Description = elements[CHEAT_CODE_HEX_TYPE_DESCRIPTION];

            return true;
        }
        public override void Refresh()
        {
            if (value_.Length % 2 != 0)
            {
                value_ += "0";
            }

            value_ = MemoryHelper.bytes_to_hex_string(MemoryHelper.ReadMemory(ulong.Parse(Address, NumberStyles.HexNumber), value_.Length/2));
        }

        public override string Save()
        {
            ulong addressDec = ulong.Parse(Address, NumberStyles.HexNumber);
            MappedSection mappedSection =  processManager.GetMappedSection(addressDec);
            string save_buf = "";
            save_buf += "hex|";
            save_buf += SectionID + "|";
            save_buf += String.Format("{0:X}", addressDec - mappedSection.Start) + "|";
            save_buf += MemoryHelper.GetStringOfValueType(Type) + "|";
            save_buf += Value + "|";
            save_buf += "0";
            save_buf += Description + "|\n";
            return save_buf;
        }
    }

    class CheatList
    {
        private List<Cheat> cheat_list;

        private const int CHEAT_CODE_HEADER_VERSION = 0;
        private const int CHEAT_CODE_HEADER_PROCESS_NAME = 1;
        private const int CHEAT_CODE_HEADER_ELEMENT_COUNT = CHEAT_CODE_HEADER_PROCESS_NAME + 1;

        private const int CHEAT_CODE_TYPE = 0;
        public CheatList()
        {
            cheat_list = new List<Cheat>();
        }

        public void Add(Cheat cheat)
        {
            cheat_list.Add(cheat);
        }

        public void RemoveAt(int idx)
        {
            cheat_list.RemoveAt(idx);
        }

        public bool Exist(Cheat cheat)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (cheat_list[i].Address == cheat.Address)
                {
                    return true;
                }
            }
            return false;
        }

        public bool LoadFile(string path, string processName, ProcessManager processManager)
        {
            string[] cheats = File.ReadAllLines(path);

            if (cheats.Length < 2)
            {
                return false;
            }

            string header = cheats[0];
            string[] header_items = header.Split('|');

            if (header_items.Length < CHEAT_CODE_HEADER_ELEMENT_COUNT)
            {
                return false;
            }

            string[] version = (header_items[CHEAT_CODE_HEADER_VERSION]).Split('.');

            ulong major_version = 0;
            ulong secondary_version = 0;

            ulong.TryParse(version[0], out major_version);
            if (version.Length > 1)
            {
                ulong.TryParse(version[1], out secondary_version);
            }

            if (major_version == 1 && secondary_version <= 3)
            {
                string process_name = header_items[CHEAT_CODE_HEADER_PROCESS_NAME];
                if (process_name != processName)
                {
                    MessageBox.Show("Invalid process.");
                    return false;
                }

                for (int i = 1; i < cheats.Length; ++i)
                {
                    string cheat_tuple = cheats[i];
                    try
                    {
                        string[] cheat_elements = cheat_tuple.Split('|');

                        if (cheat_elements.Length == 0)
                        {
                            continue;
                        }
                        Cheat cheat = null;
                        if (cheat_elements[CHEAT_CODE_TYPE] == "data")
                        {
                            cheat = new DataCheat(processManager);
                            if (cheat.Load(cheat_elements, processManager) && !Exist(cheat))
                            {

                                cheat_list.Add(cheat);
                            }
                        }
                        else if (cheat_elements[CHEAT_CODE_TYPE] == "hex")
                        {
                            cheat = new HexCheat(processManager);
                            if (cheat.Load(cheat_elements, processManager) && !Exist(cheat))
                            {
                                cheat_list.Add(cheat);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Invaid cheat file.");
                            continue;
                        }
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Invalid version.");
                return false;
            }
            return true;
        }

        public void SaveFile(string path, string prcessName, ProcessManager processManager)
        {
            string save_buf = CONSTANT.MAJOR_VERSION + "." + CONSTANT.SECONDARY_VERSION + "|" + prcessName + "\n";

            for (int i = 0; i < cheat_list.Count; ++i)
            {
                save_buf += cheat_list[i].Save();
            }

            StreamWriter myStream = new StreamWriter(path);
            myStream.Write(save_buf);
            myStream.Close();
        }

        public Cheat this[int index]
        {
            get
            {
                return cheat_list[index];
            }
            set
            {
                cheat_list[index] = value;
            }
        }

        public void Clear()
        {
            cheat_list.Clear();
        }

        public int Count { get { return cheat_list.Count; } }
    }
}
