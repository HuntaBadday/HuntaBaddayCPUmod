using System;
using System.Collections.Generic;
using System.IO;
using HuntaBaddayCPUmod.CustomData;
using LogicWorld.Server.Circuitry;

namespace HuntaBaddayCPUmod {
    public class TerminalController : LogicComponent<ITermControllerData> {
        public override bool HasPersistentValues => true;

        const int DATAIN = 0;
        const int WRITE = 8;
        const int RESET = 9;
        
        const int COLUMNSOUT = 0;
        const int ROWSOUT = 6;
        const int CHARACTEROUT = 12;
        const int CONTROLOUT = 20;
        const int BELL = 28;
        
        bool loadFromSave = false;
        
        List<Action> actions = new List<Action>();
        
        int sizeX;
        int sizeY;
        
        int cursorX = 0;
        int cursorY = 0;
        int cursorXSave = 0;
        int cursorYSave = 0;
        
        bool cursorShouldBlink = true;
        bool reverseText = false;
        bool escaped = false;
        bool csi = false;
        
        List<int> csiParameters = new List<int>();
        int currentParamVal;
        bool paramValStarted = false;
        
        bool lastWriteState = false;
        
        protected override void DoLogicUpdate() {
            Outputs[BELL].On = false;
            bool write = Inputs[WRITE].On && !lastWriteState;
            lastWriteState = Inputs[WRITE].On;
            if (write) doWrite();
            DoNextAction();
            
            if (Inputs[RESET].On) {
                if (actions.Count > 0) actions.Clear();
                cursorShouldBlink = true;
                reverseText = false;
                cursorX = 0;
                cursorY = 0;
                writeData(0, COLUMNSOUT);
                writeData(0, ROWSOUT);
                writeData(0, CHARACTEROUT);
                writeData(0b1101, CONTROLOUT);
            }
        }
        
        void doWrite() {
            byte data = readDataIn();
            if (escaped) {
                doEscape(data);
                return;
            }
            if (data >= 0x20) {
                printCharacter(data);
            } else {
                switch (data) {
                    case 0x07:
                        Outputs[BELL].On = true;
                        QueueLogicUpdate();
                        break;
                    case 0x08:
                        backSpace();
                        break;
                    case 0x0a:
                        carriageReturn();
                        break;
                    case 0x0c:
                        formFeed();
                        break;
                    case 0x1b:
                        escaped = true;
                        break;
                }
            }
        }
        
        void doEscape(byte data) {
            if (!csi) {
                if (data == 0x5b) { // '['
                    csi = true;
                    csiParameters.Clear();
                    currentParamVal = 0;
                    paramValStarted = false;
                } else {
                    escaped = false;
                }
                return;
            }
            
            if (data >= 0x30 && data <= 0x39) { // '0' - '9'
                currentParamVal *= 10;
                currentParamVal += data-0x30;
                paramValStarted = true;
            } else if (data == 0x3b) { // ';'
                csiParameters.Add(currentParamVal);
                currentParamVal = 0;
                paramValStarted = false;
            } else if (data >= 0x40 && data <= 0x7e) { // '@' - '~'
                if (paramValStarted) csiParameters.Add(currentParamVal);
                endCSI(data);
                escaped = false;
                csi = false;
            }
            
            if (csiParameters.Count >= 64) {
                escaped = false;
                csi = false;
            }
        }
        
        void endCSI(byte data) {
            bool csiImpliedValue = csiParameters.Count == 0;
            switch (data) {
                case 0x41: // 'A'
                    cursorY -= csiImpliedValue ? 1 : csiParameters[0];
                    if (cursorY < 0) cursorY = 0;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x42: // 'B'
                    cursorY += csiImpliedValue ? 1 : csiParameters[0];
                    if (cursorY >= sizeY) cursorY = sizeY-1;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x43: // 'C'
                    cursorX += csiImpliedValue ? 1 : csiParameters[0];
                    if (cursorX >= sizeX) cursorX = sizeX-1;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x44: // 'D'
                    cursorX -= csiImpliedValue ? 1 : csiParameters[0];
                    if (cursorX < 0) cursorX = 0;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x45: // 'E'
                    cursorY += csiImpliedValue ? 1 : csiParameters[0];
                    if (cursorY >= sizeY) cursorY = sizeY-1;
                    cursorX = 0;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x46: // 'F'
                    cursorY -= csiImpliedValue ? 1 : csiParameters[0];
                    if (cursorY < 0) cursorY = 0;
                    cursorX = 0;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x47: // 'G'
                    cursorX = csiImpliedValue ? 0 : csiParameters[0]-1;
                    if (cursorX < 0) cursorX = 0;
                    if (cursorX >= sizeX) cursorX = sizeX-1;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x48: // 'H'
                    cursorY = csiParameters.Count > 0 ? csiParameters[0]-1 : 0;
                    cursorX = csiParameters.Count > 1 ? csiParameters[1]-1 : 0;
                    if (cursorX < 0) cursorX = 0;
                    if (cursorX >= sizeX) cursorX = sizeX-1;
                    if (cursorY < 0) cursorY = 0;
                    if (cursorY >= sizeY) cursorY = sizeY-1;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x4a: { // 'J'
                    int n = csiImpliedValue ? 0 : csiParameters[0];
                    bool first = true;
                    switch (n) {
                        case 0:
                            for (int y = cursorY; y < sizeY; y++) {
                                for (int x = first ? cursorX : 0; x < sizeX; x++) {
                                    makeAction((byte)x, (byte)y, 0x20, 0b10);
                                }
                                first = false;
                            }
                            break;
                        case 1:
                            for (int y = cursorY; y >= 0; y--) {
                                for (int x = first ? cursorX : sizeX-1; x >= 0; x--) {
                                    makeAction((byte)x, (byte)y, 0x20, 0b10);
                                }
                                first = false;
                            }
                            break;
                        case 2:
                            makeAction(0, 0, 0, 0b1);
                            break;
                    }
                    break;
                }
                case 0x4b: { // 'K'
                    int n = csiImpliedValue ? 0 : csiParameters[0];
                    switch (n) {
                        case 0:
                            for (int x = cursorX; x < sizeX; x++) {
                                makeAction((byte)x, (byte)cursorY, 0x20, 0b10);
                            }
                            break;
                        case 1:
                            for (int x = cursorX; x >= 0; x--) {
                                makeAction((byte)x, (byte)cursorY, 0x20, 0b10);
                            }
                            break;
                        case 2:
                            for (int x = 0; x < sizeX; x++) {
                                makeAction((byte)x, (byte)cursorY, 0x20, 0b10);
                            }
                            break;
                    }
                    break;
                }
                case 0x53: // 'S'
                    for (int i = 0; i < (csiImpliedValue ? 1 : csiParameters[0] <= 256 ? csiParameters[0] : 256); i++) {
                        makeAction(0, 0, 0, 0b10000);
                        makeAction(0, 0, 0, 0b0);
                    }
                    break;
                case 0x54: // 'T'
                    for (int i = 0; i < (csiImpliedValue ? 1 : csiParameters[0] <= 256 ? csiParameters[0] : 256); i++) {
                        makeAction(0, 0, 0, 0b100000);
                        makeAction(0, 0, 0, 0b0);
                    }
                    break;
                case 0x66: // 'f'
                    cursorY = csiParameters.Count > 0 ? csiParameters[0]-1 : 0;
                    cursorX = csiParameters.Count > 1 ? csiParameters[1]-1 : 0;
                    if (cursorX < 0) cursorX = 0;
                    if (cursorX >= sizeX) cursorX = sizeX-1;
                    if (cursorY < 0) cursorY = 0;
                    if (cursorY >= sizeY) cursorY = sizeY-1;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
                case 0x6d: // 'm'
                    if (csiImpliedValue) csiParameters.Add(0);
                    for (int i = 0; i < csiParameters.Count; i++) {
                        switch (csiParameters[i]) {
                            case 0:
                                cursorShouldBlink = true;
                                reverseText = false;
                                break;
                            case 3:
                                cursorShouldBlink = true;
                                break;
                            case 7:
                                reverseText = true;
                                break;
                            case 25:
                                cursorShouldBlink = false;
                                break;
                            case 27:
                                reverseText = false;
                                break;
                        }
                    }
                    makeAction(0, 0, 0, 0);
                    break;
                case 0x73: // 's'
                    cursorXSave = cursorX;
                    cursorYSave = cursorY;
                    break;
                case 0x75: // 'u'
                    cursorX = cursorXSave;
                    cursorY = cursorYSave;
                    if (cursorX >= sizeX) cursorX = sizeX-1;
                    if (cursorY >= sizeY) cursorY = sizeY-1;
                    makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
                    break;
            }
        }
        
        void formFeed() {
            cursorX = 0;
            cursorY = 0;
            makeAction(0, 0, 0, 0b101);
        }
        
        void backSpace() {
            cursorX--;
            if (cursorX < 0) {
                cursorX = cursorY == 0 ? 0 : sizeX-1;
                if (cursorY > 0) cursorY--;
            }
            makeAction((byte)cursorX, (byte)cursorY, 0x20, 0b110);
        }
        
        void carriageReturn() {
            cursorX = 0;
            cursorY++;
            if (cursorY >= sizeY) {
                makeAction(0, 0, 0, 0b10000);
                cursorY = sizeY-1;
            }
            makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
        }
        
        void printCharacter(byte character) {
            makeAction((byte)cursorX, (byte)cursorY, (byte)(character^(reverseText ? 0x80 : 0)), 0b10);
            advanceCursor();
            makeAction((byte)cursorX, (byte)cursorY, 0, 0b100);
        }
        
        void makeAction(byte column, byte row, byte character, byte control) {
            Action a = new Action(column, row, character, control);
            actions.Add(a);
        }
        
        void advanceCursor() {
            cursorX++;
            if (cursorX >= sizeX) {
                cursorX = 0;
                cursorY++;
            }
            if (cursorY >= sizeY) {
                makeAction(0, 0, 0, 0b10000);
                cursorY = sizeY-1;
            }
        }
        
        void DoNextAction() {
            if (actions.Count == 0) return;
            Action a = actions[0];
            actions.RemoveAt(0);
            
            if (cursorShouldBlink) a.control |= 0b1000;
            
            writeData(a.column, COLUMNSOUT);
            writeData(a.row, ROWSOUT);
            writeData(a.character, CHARACTEROUT);
            writeData(a.control, CONTROLOUT);
            
            if (actions.Count != 0) QueueLogicUpdate();
        }
        
        byte readDataIn() {
            byte output = 0;
            for (int i = 0; i < 8; i++) {
                output >>= 1;
                output |= (byte)(Inputs[DATAIN+i].On ? 0x80 : 0);
            }
            return output;
        }
        
        void writeData(byte data, int pin) {
            for (int i = 0; i < 8; i++) {
                Outputs[pin+i].On = (data&0x1) == 1;
                data >>= 1;
            }
        }

        protected override void Initialize() {
            loadFromSave = true;
        }

        protected override void OnCustomDataUpdated() {
            /*
            int cursorX = 0;
            int cursorY = 0;
            int cursorXSave = 0;
            int cursorYSave = 0;

            bool cursorShouldBlink = true;
            bool reverseText = false;
            bool escaped = false;
            bool csi = false;

            int currentParamVal;
            bool paramValStarted = false;

            bool lastWriteState = false;
            
            List<Action> actions = new List<Action>();
            List<int> csiParameters = new List<int>();
             */
             
            if (loadFromSave) {
                loadFromSave = false;
                if (Data.Data.Length > 0) {
                    try {
                        MemoryStream input = new MemoryStream(Data.Data);
                        BinaryReader reader = new BinaryReader(input);
                        
                        cursorX = reader.ReadInt32();
                        cursorY = reader.ReadInt32();
                        cursorXSave = reader.ReadInt32();
                        cursorYSave = reader.ReadInt32();
                        
                        cursorShouldBlink = reader.ReadBoolean();
                        reverseText = reader.ReadBoolean();
                        escaped = reader.ReadBoolean();
                        csi = reader.ReadBoolean();
                        
                        currentParamVal = reader.ReadInt32();
                        paramValStarted = reader.ReadBoolean();
                        
                        lastWriteState = reader.ReadBoolean();
                        
                        int n = reader.ReadInt32();
                        for (int i = 0; i < n; i++) {
                            Action a = new Action(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                            actions.Add(a);
                        }
                        
                        n = reader.ReadInt32();
                        for (int i = 0; i < n; i++) {
                            csiParameters.Add(reader.ReadInt32());
                        }
                    } catch (Exception e) {
                        Logger.Error("Terminal Controller: Failed to load custom data: "+e);
                    }
                }
            }
            
            sizeX = Data.Width;
            sizeY = Data.Height;
            if (cursorX >= sizeX) cursorX = sizeX-1;
            if (cursorY >= sizeY) cursorY = sizeY-1;
        }

        protected override void SavePersistentValuesToCustomData() {
            /*
            int cursorX = 0;
            int cursorY = 0;
            int cursorXSave = 0;
            int cursorYSave = 0;

            bool cursorShouldBlink = true;
            bool reverseText = false;
            bool escaped = false;
            bool csi = false;

            int currentParamVal;
            bool paramValStarted = false;

            bool lastWriteState = false;
            
            List<Action> actions = new List<Action>();
            List<int> csiParameters = new List<int>();
             */
            
            MemoryStream outputStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(outputStream);
            
            writer.Write(cursorX);
            writer.Write(cursorY);
            writer.Write(cursorXSave);
            writer.Write(cursorYSave);
            
            writer.Write(cursorShouldBlink);
            writer.Write(reverseText);
            writer.Write(escaped);
            writer.Write(csi);
            
            writer.Write(currentParamVal);
            writer.Write(paramValStarted);
            writer.Write(lastWriteState);
            
            writer.Write(actions.Count);
            for (int i = 0; i < actions.Count; i++) {
                writer.Write(actions[i].column);
                writer.Write(actions[i].row);
                writer.Write(actions[i].character);
                writer.Write(actions[i].control);
            }
            
            writer.Write(csiParameters.Count);
            for (int i = 0; i < csiParameters.Count; i++) {
                writer.Write(csiParameters[i]);
            }
            
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