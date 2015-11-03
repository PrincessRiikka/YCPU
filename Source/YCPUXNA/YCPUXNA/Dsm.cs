﻿using System.IO;
using Ypsilon.Hardware;

namespace YCPUXNA
{
    class Dsm
    {
        public bool TryDisassemble(string[] args)
        {
            string[] disassembly = null;

            if (args.Length <= 1)
                return false;

            string inPath = args[1];
            string outPath = inPath + "asm";

            if (File.Exists(inPath))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(inPath, FileMode.Open)))
                {
                    disassembly = Disassemble(reader);
                }
                File.WriteAllLines(outPath, disassembly);
                return true;
            }
            else
            {
                return false;
            }
        }

        private string[] Disassemble(BinaryReader reader)
        {
            YCPU ycpu = new YCPU();
            LoadBinaryToCPU(reader, ycpu);

            string[] disassembled;
            disassembled = ycpu.Disassemble(0x0000, 32000, false);

            return disassembled;
        }

        private void LoadBinaryToCPU(BinaryReader reader, YCPU ycpu)
        {
            ushort address = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ycpu.WriteMemInt8((ushort)(address),reader.ReadByte());
                address += 1;
            }
        }
    }
}