using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;
using HuntaBaddayCPUmod.CustomData;
using LogicWorld.Server.Circuitry;
using System.Timers;

namespace HuntaBaddayCPUmod {
    public class SplitFlapController : LogicComponent<ISplitFlapControllerData> {
        public override bool HasPersistentValues => true;
        const int COLUMNS = 0;
        const int ROWS = 6;
        const int CHARACTER = 12;
        const int CONTROL = 20;
        const int FLIP = 28;
        const int RESET = 29;
        
        bool loadFromSave = false;
        
        byte[] screenBuffer = new byte[4096];
        byte[] realScreen = new byte[4096];
        
        int flipAmount;
        
        int cursorX;
        int cursorY;
        
        List<Action> actions = new List<Action>();
        
        byte controlState;
        
        protected override void Initialize() {
            loadFromSave = true;
        }
        
        protected override void DoLogicUpdate() {
            byte row = readRows();
            byte column = readColumns();
            controlState = readControl();
            
            if ((controlState&0b1) != 0) {
                for (int i = 0; i < 4096; i++) {
                    screenBuffer[i] = 0x20; // ' '
                }
            }
            if ((controlState & 0b10) != 0) {
                screenBuffer[row*64 + column] = readCharacter();
            }
            if ((controlState & 0b100) != 0) {
                if (column != cursorX || row != cursorY) {
                    makeAction(column, row, 0, 0b100, true);
                }
                cursorX = column;
                cursorY = row;
            }
            if ((controlState & 0b10000) != 0) {
                for (int y = 1; y < 64; y++) {
                    for (int x = 0; x < 64; x++) {
                        int index1 = y*64 + x;
                        int index2 = (y-1)*64 + x;
                        screenBuffer[index2] = screenBuffer[index1];
                    }
                }
                for (int x = 0; x < 64; x++) {
                    screenBuffer[4032+x] = 0x20;
                }
            }
            if ((controlState & 0b100000) != 0) {
                for (int y = 62; y >= 0; y--) {
                    for (int x = 0; x < 64; x++) {
                        int index1 = y*64 + x;
                        int index2 = (y+1)*64 + x;
                        screenBuffer[index2] = screenBuffer[index1];
                    }
                }
                for (int x = 0; x < 64; x++) {
                    screenBuffer[x] = 0x20;
                }
            }
            if ((controlState & 0b1000000) != 0) {
                for (int y = 0; y < 64; y++) {
                    for (int x = 1; x < 64; x++) {
                        int index1 = y*64 + x;
                        int index2 = y*64 + x-1;
                        screenBuffer[index2] = screenBuffer[index1];
                    }
                }
                for (int y = 0; y < 64; y++) {
                    screenBuffer[y*64+63] = 0x20;
                }
            }
            if ((controlState & 0b10000000) != 0) {
                for (int y = 0; y < 64; y++) {
                    for (int x = 62; x >= 0; x--) {
                        int index1 = y*64 + x;
                        int index2 = y*64 + x+1;
                        screenBuffer[index2] = screenBuffer[index1];
                    }
                }
                for (int y = 0; y < 64; y++) {
                    screenBuffer[y*64] = 0x20;
                }
            }
            
            if (Inputs[FLIP].On) {
                for (int y = 0; y < 64; y++) {
                    for (int x = 0; x < 64; x++) {
                        int i = y*64 + x;
                        byte sb = screenBuffer[i];
                        if (screenBuffer[i] != realScreen[i]) {
                            realScreen[i] += (byte)flipAmount;
                            if (sb < realScreen[i] && realScreen[i]-sb < flipAmount) realScreen[i] = sb;
                            makeAction((byte)x, (byte)y, realScreen[i], 0b10, false);
                        }
                    }
                }
            }
            
            if (Inputs[RESET].On) {
                for (int i = 0; i < 4096; i++) {
                    screenBuffer[i] = 0x20; // ' '
                    realScreen[i] = 0x20; // ' '
                }
                actions.Clear();
                makeAction(0, 0, 0, 0b1, false);
            }
            
            if (actions.Count == 0) {
                writeData((byte)(controlState&0b1000), CONTROL);
            }
            
            DoNextAction();
        }
        
        void makeAction(byte column, byte row, byte character, byte control, bool force) {
            Action a = new Action(column, row, character, control);
            if (force) {
                actions.Insert(0, a);
            } else {
                actions.Add(a);
            }
        }
        
        void DoNextAction() {
            if (actions.Count == 0) return;
            Action a = actions[0];
            actions.RemoveAt(0);
            
            a.control |= (byte)(controlState&0b1000);
            writeData(a.column, COLUMNS);
            writeData(a.row, ROWS);
            writeData(a.character, CHARACTER);
            writeData(a.control, CONTROL);
            
            if (actions.Count != 0) QueueLogicUpdate();
        }
        
        byte readColumns() {
            byte output = 0;
            for (int i = 0; i < 6; i++) {
                output >>= 1;
                output |= (byte)(Inputs[COLUMNS+i].On ? 0x20 : 0);
            }
            return output;
        }
        
        byte readRows() {
            byte output = 0;
            for (int i = 0; i < 6; i++) {
                output >>= 1;
                output |= (byte)(Inputs[ROWS+i].On ? 0x20 : 0);
            }
            return output;
        }
        
        byte readCharacter() {
            byte output = 0;
            for (int i = 0; i < 8; i++) {
                output >>= 1;
                output |= (byte)(Inputs[CHARACTER+i].On ? 0x80 : 0);
            }
            return output;
        }
        
        byte readControl() {
            byte output = 0;
            for (int i = 0; i < 8; i++) {
                output >>= 1;
                output |= (byte)(Inputs[CONTROL+i].On ? 0x80 : 0);
            }
            return output;
        }
        
        void writeData(byte data, int pin) {
            for (int i = 0; i < 8; i++) {
                Outputs[pin+i].On = (data&0x1) == 1;
                data >>= 1;
            }
        }
        
        protected override void OnCustomDataUpdated() {
            /*
            int cursorX;
            int cursorY;
            
            byte[] screenBuffer = new byte[4096];
            byte[] realScreen = new byte[4096];
            List<Action> actions = new List<Action>();
             */
            if (loadFromSave) {
                loadFromSave = false;
                if (Data.Data.Length > 0) {
                    try {
                        MemoryStream memstream = new MemoryStream(Data.Data);
                        DeflateStream decompressor = new DeflateStream(memstream, CompressionMode.Decompress);
                        BinaryReader reader = new BinaryReader(decompressor);
                        
                        cursorX = reader.ReadInt32();
                        cursorY = reader.ReadInt32();
                        
                        for (int i = 0; i < 4096; i++) {
                            screenBuffer[i] = reader.ReadByte();
                            realScreen[i] = reader.ReadByte();
                        }
                        
                        int n = reader.ReadInt32();
                        for (int i = 0; i < n; i++) {
                            Action a = new Action(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                            actions.Add(a);
                        }
                        
                    } catch (Exception e) {
                        Logger.Error("Split Flap Controller: Failed to load custom data: "+e);
                    }
                    Data.Data = new byte[0];
                }
            }
            flipAmount = Data.FlipAmount;
        }

        protected override void SavePersistentValuesToCustomData() {
            /*
            int cursorX;
            int cursorY;
            
            byte[] screenBuffer = new byte[4096];
            byte[] realScreen = new byte[4096];
            List<Action> actions = new List<Action>();
             */
            
            MemoryStream outputStream = new MemoryStream();
            DeflateStream compressor = new DeflateStream(outputStream, CompressionLevel.Optimal, true);
            BinaryWriter writer = new BinaryWriter(compressor);
            
            writer.Write(cursorX);
            writer.Write(cursorY);
            
            for (int i = 0; i < 4096; i++) {
                writer.Write(screenBuffer[i]);
                writer.Write(realScreen[i]);
            }
            
            writer.Write(actions.Count);
            for (int i = 0; i < actions.Count; i++) {
                writer.Write(actions[i].column);
                writer.Write(actions[i].row);
                writer.Write(actions[i].character);
                writer.Write(actions[i].control);
            }
            
            compressor.Flush();
            Data.Data = outputStream.ToArray();
        }

        protected override void SetDataDefaultValues() {
            Data.Initialize();
        }
        
        class Action {
            public byte column;
            public byte row;
            public byte character;
            public byte control;
            
            public Action(byte column, byte row, byte character, byte control) {
                this.column = column;
                this.row = row;
                this.character = character;
                this.control = control;
            }
        }
    }
}