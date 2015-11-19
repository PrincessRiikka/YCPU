﻿
namespace Ypsilon.Emulation.Hardware
{
    partial class YCPU
    {
        public string[] Disassemble(ushort address, int count, bool extendedFormat = true)
        {
            string[] s = new string[count];
            ushort word;

            for (int i = 0; i < count; i += 1)
            {
                word = DebugReadMemory(address);
                ushort nextword = DebugReadMemory((ushort)(address + 2));
                bool usesNextWord = false;

                YCPUInstruction opcode = Opcodes[word & 0x00FF];
                if (extendedFormat)
                {
                    s[i] = string.Format("{0:X4}:{1:X4} {2}",
                        address, word, (opcode.Disassembler != null) ?
                        opcode.Disassembler(opcode.Name, word, nextword, address, true, out usesNextWord) :
                        opcode.Name);
                }
                else
                {
                    s[i] = (opcode.Disassembler != null) ?
                        opcode.Disassembler(opcode.Name, word, nextword, address, false, out usesNextWord).ToLowerInvariant() :
                        opcode.Name;
                }
                address += (ushort)(usesNextWord ? 4 : 2);
            }
            return s;
        }

        private string DisassembleALU(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            int addressingmode = (operand & 0xF000) >> 12;
            RegGPIndex regDest = (RegGPIndex)((operand & 0x0007));
            RegGPIndex regSrc = (RegGPIndex)((operand & 0x0E00) >> 9);
            bool isEightBit = (operand & 0x0100) != 0;
            int regIndex = ((operand & 0x7000) >> 12);

            switch (addressingmode)
            {
                case 0: // Immediate or Absolute
                    bool absolute = (operand & 0x0200) != 0;
                    if (name == "STO" && absolute == false)
                    {
                        usesNextWord = false;
                        return "???";
                    }
                    else
                    {
                        usesNextWord = true;
                        string disasm = string.Format("{0,-8}{1}, {3}${2:X4}{4}",
                            name + (isEightBit ? ".8" : string.Empty),
                            NameOfRegGP(regDest),
                            nextword,
                            absolute ? "[" : string.Empty, absolute ? "]" : string.Empty);
                        if (showMemoryContents)
                            disasm = AppendMemoryContents(disasm, absolute ? DebugReadMemory(nextword) : nextword);
                        return disasm;
                    }
                case 1: // Status Register
                    usesNextWord = false;
                    {
                        string disasm = string.Format("{0,-8}{1}, {2,-12}",
                            name,
                            NameOfRegGP(regDest),
                            NameOfRegSP((RegSPIndex)((int)regSrc)));
                        if (showMemoryContents)
                            disasm = AppendMemoryContents(disasm, ReadStatusRegister((RegSPIndex)regSrc));
                        return disasm;
                    }
                case 2: // Register
                    usesNextWord = false;
                    return string.Format("{0,-8}{1}, {2,-12}(${3:X4})",
                        name + (isEightBit ? ".8" : string.Empty), 
                        NameOfRegGP(regDest),
                        NameOfRegGP((RegGPIndex)((int)regSrc)),
                        R[(int)regSrc]);

                case 3: // Indirect
                    usesNextWord = false;
                    return string.Format("{0,-8}{1}, [{2}]        (${3:X4})",
                        name + (isEightBit ? ".8" : string.Empty), 
                        NameOfRegGP(regDest),
                        NameOfRegGP((RegGPIndex)((int)regSrc)),
                        DebugReadMemory(R[(int)regSrc]));

                case 4: // Indirect Offset (also Absolute Offset)
                    usesNextWord = true;
                    return string.Format("{0,-8}{1}, [{2},${3:X4}]  (${4:X4})",
                        name + (isEightBit ? ".8" : string.Empty), 
                        NameOfRegGP(regDest),
                        NameOfRegGP((RegGPIndex)((int)regSrc)), 
                        nextword,
                        DebugReadMemory((ushort)(R[(int)regSrc] + nextword)));

                case 5: // Stack offset
                    usesNextWord = true;
                    return string.Format("{0,-8}{1}, S[${2:X1}]       (${3:X4})",
                        name + (isEightBit ? ".8" : string.Empty), 
                        NameOfRegGP(regDest), 
                        (int)regSrc,
                        DebugReadMemory((ushort)(R[(int)regSrc] + nextword)));

                case 6: // Indirect PostInc
                    usesNextWord = false;
                    return string.Format("{0,-8}{1}, [{2}+]       (${3:X4})",
                        name + (isEightBit ? ".8" : string.Empty), 
                        NameOfRegGP(regDest),
                        NameOfRegGP((RegGPIndex)((int)regSrc)),
                        DebugReadMemory(R[(int)regSrc]));

                case 7: // Indirect PreDec
                    usesNextWord = false;
                    return string.Format("{0,-8}{1}, [-{2}]       (${3:X4})",
                        name + (isEightBit ? ".8" : string.Empty), 
                        NameOfRegGP(regDest),
                        NameOfRegGP((RegGPIndex)((int)regSrc)),
                        DebugReadMemory(R[(int)regSrc]));

                default: // $8 - $F are Indirect Indexed
                    usesNextWord = false;
                    return string.Format("{0,-8}{1}, [{2},{3}]     (${4:X4})",
                        name + (isEightBit ? ".8" : string.Empty), 
                        NameOfRegGP(regDest),
                        NameOfRegGP((RegGPIndex)((int)regSrc)),
                        NameOfRegGP((RegGPIndex)regIndex),
                        DebugReadMemory((ushort)(R[(int)regSrc] + R[regIndex])));

            }
        }

        private string DisassembleBTT(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            RegGPIndex destination = (RegGPIndex)((operand & 0xE000) >> 13);
            bool as_register = ((operand & 0x1000) != 0);
            ushort value = (as_register) ?
                (ushort)((operand & 0x0700) >> 8) :
                (ushort)((operand & 0x0F00) >> 8);
            return string.Format("{0,-8}{1}, {2}", name, NameOfRegGP(destination), as_register ?
                string.Format("{0,-8}(${1:X1})", NameOfRegGP((RegGPIndex)value), R[value]) :
                string.Format("${0:X1}", value));
        }

        private string DisassembleBRA(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            sbyte value = (sbyte)((operand & 0xFF00) >> 8);
            return string.Format("{0,-8}{3}{1:000}            (${2:X4})", name, value, (ushort)(address + value), (value & 0x80) == 0 ? "+" : string.Empty);
        }

        private string DisassembleFLG(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            string flags = string.Format("{0}{1}{2}{3}",
                ((operand & 0x8000) != 0) ? "N " : string.Empty,
                ((operand & 0x4000) != 0) ? "Z " : string.Empty,
                ((operand & 0x2000) != 0) ? "C " : string.Empty,
                ((operand & 0x1000) != 0) ? "V" : string.Empty);
            if (flags == string.Empty)
                flags = "<NONE>";
            return string.Format("{0,-8}{1}", name, flags);
        }

        private string DisassembleHWQ(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            RegGPIndex unused;
            ushort value;
            BitPatternHWQ(operand, out value, out unused);

            return string.Format("{0,-8}${1:X2}", name, value);
        }

        private string DisassembleINC(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            RegGPIndex destination;
            ushort value;
            BitPatternIMM(operand, out value, out destination);

            return string.Format("{0,-8}{1}, ${2:X2}", name, NameOfRegGP(destination), value);
        }

        private string DisassembleJMP(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            int addressingmode = ((operand & 0xF000) >> 12);
            RegGPIndex r_src = (RegGPIndex)((operand & 0x1C00) >> 10);
            int index_bits = ((operand & 0x0300) >> 8);

            switch (addressingmode)
            {
                case 0: // Immediate
                    usesNextWord = true;
                    bool absolute = (operand & 0x0100) != 0;
                    return string.Format("{0,-8}{2}${1:X4}{3}{4}", name, nextword,
                        absolute ? "[" : string.Empty, absolute ? "]" : string.Empty,
                        absolute ? string.Format("         (${0:X4})", DebugReadMemory(nextword)) : string.Empty);
                case 1: // Register
                    usesNextWord = false;
                    return string.Format("{0,-8}{1}              (${2:X4})", name,
                        NameOfRegGP((RegGPIndex)((int)r_src)),
                        R[(int)r_src]);
                case 2: // Indirect
                    usesNextWord = false;
                    return string.Format("{0,-8}[{1}]            (${2:X4})", name,
                        NameOfRegGP((RegGPIndex)((int)r_src)),
                        DebugReadMemory(R[(int)r_src]));
                case 3: // Indirect Offset (also Absolute Offset)
                    usesNextWord = true;
                    return string.Format("{0,-8}[{1},${2:X4}]      (${3:X4})", name,
                        NameOfRegGP((RegGPIndex)((int)r_src)), nextword,
                        DebugReadMemory((ushort)(R[(int)r_src] + nextword)));
                case 4: // Indirect PostInc
                    usesNextWord = false;
                    return string.Format("{0,-8}[{1}+]           (${2:X4})", name,
                        NameOfRegGP((RegGPIndex)((int)r_src)),
                        DebugReadMemory(R[(int)r_src]));
                case 5: // Indirect PreDec
                    usesNextWord = false;
                    return string.Format("{0,-8}[-{1}]           (${2:X4})", name,
                        NameOfRegGP((RegGPIndex)((int)r_src)),
                        DebugReadMemory(R[(int)r_src]));
                case 6: // Indirect Indexed
                    usesNextWord = false;
                    return string.Format("{0,-8}[{1},{2}]         (${3:X4})", name,
                        NameOfRegGP((RegGPIndex)((int)r_src)),
                        NameOfRegGP((RegGPIndex)index_bits),
                        DebugReadMemory((ushort)
                            (R[(int)r_src] + R[index_bits])));
                case 7:
                    usesNextWord = false;
                    return string.Format("{0,-8}[{1},{2}]         (${3:X4})", name,
                       NameOfRegGP((RegGPIndex)((int)r_src)),
                       NameOfRegGP((RegGPIndex)(index_bits + 4)),
                       DebugReadMemory((ushort)
                           (R[(int)r_src] + R[index_bits + 4])));
                default:
                    usesNextWord = false;
                    return "ERROR JMP Unsigned Format";
            }
        }

        private string DisassembleMMU(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            RegGPIndex RegMmuIndex;
            RegGPIndex RegMmuValue;
            BitPatternMMU(operand, out RegMmuIndex, out RegMmuValue);
            return string.Format("{0,-8}{1}, {2}", name, NameOfRegGP((RegGPIndex)RegMmuIndex), NameOfRegGP((RegGPIndex)RegMmuValue));
        }

        private string DisassembleNoBits(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            return string.Format(name);
        }

        private string DisassembleSTK(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            string flags = "{0}{1}{2}{3}{4}{5}{6}{7}";
            if ((operand & 0x0001) == 0x0000)
            {
                flags = string.Format(flags,
                    ((operand & 0x8000) != 0) ? "R7 " : string.Empty,
                    ((operand & 0x4000) != 0) ? "R6 " : string.Empty,
                    ((operand & 0x2000) != 0) ? "R5 " : string.Empty,
                    ((operand & 0x1000) != 0) ? "R4 " : string.Empty,
                    ((operand & 0x0800) != 0) ? "R3 " : string.Empty,
                    ((operand & 0x0400) != 0) ? "R2 " : string.Empty,
                    ((operand & 0x0200) != 0) ? "R1 " : string.Empty,
                    ((operand & 0x0100) != 0) ? "R0 " : string.Empty);
            }
            else
            {
                flags = string.Format(flags,
                    ((operand & 0x8000) != 0) ? "SP " : string.Empty,
                    ((operand & 0x4000) != 0) ? "USP " : string.Empty,
                    ((operand & 0x2000) != 0) ? "IA " : string.Empty,
                    ((operand & 0x1000) != 0) ? "II " : string.Empty,
                    ((operand & 0x0800) != 0) ? "P2 " : string.Empty,
                    ((operand & 0x0400) != 0) ? "PS " : string.Empty,
                    ((operand & 0x0200) != 0) ? "PC " : string.Empty,
                    ((operand & 0x0100) != 0) ? "FL " : string.Empty);
            }
            if (flags == string.Empty)
                flags = "<NONE>";
            if (name.ToLowerInvariant() == "pop" && (flags.Trim() == "PC"))
                return "RTS";
            return string.Format("{0,-8}{1}", name, flags);
        }

        private string DisassembleSET(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            RegGPIndex destination = (RegGPIndex)((operand & 0xE000) >> 13);
            int value = ((operand & 0x1F00) >> 8);
            if ((operand & 0x0001) == 1)
            {
                if (value <= 0x0A)
                    value = (ushort)(0x0001 << (value + 0x05));
                else
                    value = (ushort)(0xFFE0 + value);
            }
            return string.Format("{0,-8}{1}, {2}", name, NameOfRegGP(destination), string.Format("${0:X2}", value));
        }

        private string DisassembleSHF(string name, ushort operand, ushort nextword, ushort address, bool showMemoryContents, out bool usesNextWord)
        {
            usesNextWord = false;
            RegGPIndex destination = (RegGPIndex)((operand & 0xE000) >> 13);
            string value = string.Empty;
            if ((operand & 0x1000) == 0)
            {
                int shiftby = ((operand & 0x0F00) >> 8) + 1;
                value = string.Format("${0:X2}", (ushort)shiftby);
            }
            else
                value = NameOfRegGP((RegGPIndex)((operand & 0x0700) >> 8));
            return string.Format("{0,-8}{1}, {2}", name, NameOfRegGP(destination), value);
        }

        private string AppendMemoryContents(string disasm, ushort mem)
        {
            int len = disasm.Length;
            disasm = string.Format("{0}{1}(${2:X4})", disasm, new string(' ', 24 - len), mem);
            return disasm;
        }

        private string NameOfRegGP(RegGPIndex register)
        {
            switch (register)
            {
                case RegGPIndex.R0:
                    return "R0";
                case RegGPIndex.R1:
                    return "R1";
                case RegGPIndex.R2:
                    return "R2";
                case RegGPIndex.R3:
                    return "R3";
                case RegGPIndex.R4:
                    return "R4";
                case RegGPIndex.R5:
                    return "R5";
                case RegGPIndex.R6:
                    return "R6";
                case RegGPIndex.R7:
                    return "R7";
                default:
                    return "??";
            }
        }

        private string NameOfRegSP(RegSPIndex register)
        {
            switch (register)
            {
                case RegSPIndex.FL:
                    return "FL";
                case RegSPIndex.PC:
                    return "PC";
                case RegSPIndex.PS:
                    return "PS";
                case RegSPIndex.P2:
                    return "P2";
                case RegSPIndex.II:
                    return "II";
                case RegSPIndex.IA:
                    return "IA";
                case RegSPIndex.USP:
                    return "USP";
                case RegSPIndex.SSP:
                    return "SP";
                default:
                    return "??";
            }
        }
    }
}