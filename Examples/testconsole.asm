; TEST CONSOLE
; Tests segments and mmu, interrupts, etc.
; Expects Graphics Device @ bus index 1, Keyboard @ index 2, at least 128kb RAM.

.alignglobals 2                 ; align global labels to 16-bit boundaries
.alias  KeyboardData    $7000
.include "testconsole.ivt.asm"  ; include int vector table & interrupt handlers

; === ResetInt =================================================================
ResetInt:
{
    ; After the reset interrupt, the processor state is as described in 2.G.
    ; set stack pointer to $0000.
    lod     A, $0000
    sto     A, sp
    
    ;set up devices, mmu, etc.
    jsr     Setup
    
    ; draw the 'NYA ELEKTRISKA' screen, then clear
    jsr     ShowStartScreen
    jsr     ClearScreen
    ; draw top bar on screen
    lod     B, $2820       ; space char with yellow background
    lod     C, 32          ; words to write
    lod     X, $0000       ; start of video memory
    jsr     FillMemoryWords ;
    
    ; enable clock interrupt - tick at 120hz
    lod     A, 120
    hwq     $83
    
    ; use X as index to onscreen char, starting at y = 1, x = 0
    lod     X, $0040
    
    Update:
        jsr     Getc                ; A = event, or 0x0000 if no event.
        beq     Update
        
        lod     C, A              ; C = event type
        lsr     C, 8
        and     C, 0x000f
        cmp     C, 3
        bne     Update
        and     A, 0x00ff
        orr     A, 0x8200          ; yellow on blue.
        sto     A, ES[X]
        adi     X, 2
        baw     Update
}

; === ClockInt =================================================================
ClockInt:
{
    psh     A, B, C, D, fl  ; save register contents    
    hwq     $80                 ; get RTC time. seconds in low 6 bits of C.
    psh     A, B, C          ; save RTC time.
    
    and     C, $003f           ; C = hex seconds
    jsr     HexToDec            ; A = decimal seconds
    lod     B, $003c
    jsr     WriteDecToScreen
    
    pop     C
    asr     C, 8               ; C = hex minutes
    jsr     HexToDec            ; A = decimal minutes
    lod     B, $0036
    jsr     WriteDecToScreen
    
    lod     A, $283A           ; A = yellow on blue colon
    sto     A, ES[B]
    
    pop     C
    ;psh     C
    and     C, $001f
    jsr     HexToDec            ; A = decimal hours
    lod     B, $0030
    jsr     WriteDecToScreen
    
    lod     A, $283A           ; A = yellow on blue colon
    sto     A, ES[B]
    
    pop     A
    pop     A, B, C, D, fl
    rti
}

; === Getc =====================================================================
; A should be index of file descriptor. right now, does not matter, we read
; only from keyboard. Returns ascii key code in A.8, 0x00 if no key code.
Getc:
{
    psh     B, C
GetCAgain:
    lod.8   B, [KeyboardData+0]    ; B = number of events in buffer
    lod.8   C, [KeyboardData+1]    ; C = last handled event
    cmp     B, C                  ; if B == C, get new events.
    beq     getKeyboardEvents
    inc     C
    sto.8   C, [KeyboardData+1]    
    asl     C, 1
    lod     A, [KeyboardData,C]
return:
    pop     B, C
    rts
    ; === GetKeyboardEvents ====================
    ; gets all keyboard events, copies to $7000.
    ; returns: A is number of keyboard events.
    getKeyboardEvents:
        lod     A, $0002
        lod     B, $0001
        lod     C, KeyboardData
        hwq     $02
        lod     A, [KeyboardData]
        bne     GetCAgain       ; if events, then do Getc again
        baw     return          ; else return, A == 0x0000
}

; === Setup ====================================================================
; uses A, B, C, D.
Setup:
{
    ; D = calling address
    pop     D
    
    ; set up devices
    lod     A, $0001    ; set graphics adapter to LEM mode.
    lod     B, $0000
    lod     C, $0001
    hwq     $02
    lod     A, $0002    ; reset keyboard, press events only.
    lod     B, $0000
    lod     C, $0000
    hwq     $02
    
    ; set up segment registers. (See 2.F.1.)
    lod     A, $0000
    lod     B, $0800
    psh     B          ; ds = $0800 0000 (RAM @ $00000000, size = $8000)
    psh     A
    add     A, $0180
    psh     B          ; ss = $0800 0180 (RAM @ $00018000, size = $8000)
    psh     A
    lod     A, $0000
    lod     B, $8000
    psh     B          ; is = $8000 0000 (ROM @ $00000000, size = $10000)
    psh     A
    psh     B          ; cs = $8000 0000 (ROM @ $00000000, size = $10000)
    psh     A
    add     B, $0101   ; es = $8101 0000 (device 1 @ $00000000, size = $1000)
    psh     B           
    psh     A
    lsg     es
    lsg     cs
    lsg     is
    lsg     ss
    lsg     ds
    
    ; enable mmu by setting 'm' bit in PS. See 2.A.2.
    lod     A, ps
    orr     A, 0x4000
    sto     A, ps
    
    ; sp = $8000
    lod     A, $8000
    sto     A, sp
    
    ; push D (calling PC) to stack
    psh     D
    
    rts
}

; === ShowStartScreen ==========================================================
; Draws the NE logo on A blue screen
ShowStartScreen:
{
    jsr ClearScreen
    
    ; write logo in center of screen.
    lod     B, $8200            ; yellow on blue
    lod     X, $00D6            ; location in video memory
    lod     Y, txtBootText
    lod     D, 3                ; count of lines to draw
    writeLine:
        jsr WriteChars
        add     X, 64           ; increment one line in video memory (32 words)
        dec     D
        beq     LastLine
        bne     writeLine
    lastLine:
        add     X, $3C          ; skip line, align left 4 chars
        jsr     WriteChars
    rts
    
    ; boot up txt - local to this routine
    txtBootText:
    .dat8 "  \\ |  ___", 0x00
    .dat8 "|\\ \\|  ___", 0x00
    .dat8 "| \\       ", 0x00
    .dat8 "NYA ELEKTRISKA", 0x00
}

; === ClearScreen ==============================================================
ClearScreen:
{
    lod     B, $0220       ; space char with blue background
    lod     C, 384         ; words to write
    lod     X, $0000       ; start of video memory
    jsr     FillMemoryWords ; far call example: jsr.f FillMemoryWords, $80000000
    rts
}

; === WriteChars ===============================================================
; Writes A null-terminated string of 8-bit chars to video memory.
; in:   B: color
; in:   X: location to write to video memory.
; in:   Y: location to read 8-bit characters from. on exit, points to next byte after string.
WriteChars:
{
    psh     A, X              ; push A X to stack
    ssg     ds                  ; push DS register to stack
    ssg     cs
    lsg     ds                  ; DS = CS (ROM@$00000000) (reading from ROM).
    writeChar:                  ; copy chars from ds[Y] to es[X] 
        lod.8   A, DS[Y]      ; ... until ds[Y] == 0x00.
        beq     return
        orr     A, B
        sto     A, ES[X]
        inc     Y
        adi     X, 2
        baw     writeChar
    return:
        inc     Y
        lsg     ds              ; pop ds register from stack
        pop     A, X          ; pop A X from stack
        rts
}

; === FillMemoryWords ==========================================================
; Fills range of memory with words of specified value.
; in:   B - word to fill memory with
; in:   C - count of words of memory to fill    
; in:   X - first address to write to in ES
FillMemoryWords:
{
    psh     A, C, X              ; save A, C, X
    asl     C, 1                   ; C = count of bytes
    add     C, X                  ; C = first address after all bytes are written
    copyWordOneAtATime:
        sto     B, ES[X]          ; save word in B to [X], X = X + 2
        adi     X, 2
        cmp     X, C              ; if X
        bne     copyWordOneAtATime
    pop     A, C, X              ; restore A, C, X
    rts
}

; === HexToDec =================================================================
; Converts two digit hex in C to three digit decimal in A.
; Wipes out C, B, & A.
HexToDec:
{
    lod     A, $0000           ; A = 0
    lod     B, $0064           ; B = 100
    getHundreds:
        cmp     C, B
        buf     getTensBegin    ; if C < 100 goto getTensBegin
        add     A, $0100       ; else A += 100
        sub     C, B          ;   C -= 100
        baw     getHundreds
    getTensBegin:
        lod     B, $000a       ; B = 10
    getTens:
        cmp     C, B
        buf     getOnes         ; if C < 10 goto getOnes
        add     A, $0010       ; else A += 10
        sub     C, B
        baw     getTens
    getOnes:
        add     A, C
        rts
}

; === WriteDecToScreen =========================================================
; Writes two digit dec in A.8 to ES at B. Wipes out A, B += 4.
WriteDecToScreen:
{
    psh     A
    asr     A, 4
    and     A, $000f
    add     A, $2830   ; yellow on blue, char $30 + B
    sto     A, ES[B]
    adi     B, 2
    pop     A
    and     A, $000f
    add     A, $2830   ; yellow on blue, char $30 + B
    sto     A, ES[B]
    adi     B, 2
    rts
}