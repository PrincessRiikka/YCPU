; interrupt table
.dat16 start,  0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000
.dat16 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000

; boot up txt
BOOT_txt:
.dat8 "  \ |  ___"
.dat8 "|\ \|  ___" 
.dat8 "| \       "
.DAT8 "NYA ELEKTRISKA"

; Writes a string of chars to video memory.
; in:   b: color
; in:   c: count of chars to write.
; in:   x: location to write to video memory.
; in:   y: location to read 8-bit characters from.
WriteChars:
.SCOPE
    PSH A, C                ; push a and c to stack
    lod a, c                ; x += c * 2
    asl a, 1                ;
    add x, a                ;
    lod a, y                ; a = y
    add y, c                ; y += c
    lod c, a                ; c = a (original y)
    _writeChar:             
        lod.8   a, [-y]
        orr     a, b
        sto     a, [-x]
        cmp     y, c
        bne _writeChar
    pop a, c                ; pop a and c to stack
    rts
.scend

start:
.scope
    ; clear screen
    lod     b, $0220
    lod     i, 384
    lod     x, $8000
    _fillScreen:
        sto b, [x+]
        dec i
        bne _fillScreen
    ; write logo in center of screen.
    lod     b, $8200            ; black on white
    lod     x, $80D6            ; video memory
    lod     y, boot_txt
    lod     i, 3                ; count of lines to draw
    lod     c, 10
    _writeLine:
        jsr WriteChars
        add     y,10
        add     x,64
        dec     i
        beq _LastLine
        bne _writeLine
    _lastLine:
        add x, $3C ; skip line, left 4 chars
        add c, 4
        jsr WriteChars
    _infiniteLoop:
        jmp start
.scend