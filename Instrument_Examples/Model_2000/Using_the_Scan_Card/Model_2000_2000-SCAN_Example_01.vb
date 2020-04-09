﻿'================================================================================

'   Copyright 2019 Tektronix, Inc.
'   See www.tek.com/sample-license for licensing terms. 

'================================================================================

Imports Ivi.Visa.Interop

Module Module1

    Private ReadOnly echo_command As Boolean = True

    Sub Main()
        ' Create a Stopwatch object And capture the program start time from the system.
        Dim myStpWtch As Stopwatch = New Stopwatch()
        myStpWtch.Start()

        '
        ' Open the resource manager And assigns it to an object variable
        '
        Dim resource_manager As Ivi.Visa.Interop.ResourceManager = New Ivi.Visa.Interop.ResourceManager

        '
        '  Create a FormattedIO488 object to represent the instrument 
        '  you intend to communicate with, And connect to it.
        '

        Dim my_instrument As FormattedIO488 = New Ivi.Visa.Interop.FormattedIO488()
        Dim instrument_id_string As String = "USB0::0x05E6::0x6500::04397619::INSTR"
        Dim timeout As Int16 = 20000  ' define the timeout in terms of milliseconds
        ' Instrument ID String examples...
        '       LAN -> TCPIP0::134.63.71.209::inst0::INSTR
        '       USB -> USB0::0x05E6::0x2450:01419962::INSTR
        '       GPIB -> GPIB0::16::INSTR
        '       Serial -> ASRL4::INSTR
        Connect_To_Instrument(resource_manager, my_instrument, instrument_id_string, timeout)

        Instrument_Write(my_instrument, "*RST")                         ' Place the instrument into a known state
        Instrument_Write(my_instrument, "INIT:CONT OFF")                ' Disable continuous triggering
        Instrument_Write(my_instrument, "SENS:FUNC 'RES'")              ' Set the function to 2W resistance
        Instrument_Write(my_instrument, "SENS:RES:RANG:AUTO ON")        ' Enable auto ranging
        Instrument_Write(my_instrument, "SENS:RES:NPLC 1")              ' Set the integration rate to 1 PLC
        Instrument_Write(my_instrument, "SENS:RES:DIG 7")               ' Set the number of display digits to 6 ½
        Instrument_Write(my_instrument, "SENS:RES:AVER:STAT OFF")       ' Disable filtering
        Instrument_Write(my_instrument, "SENS:RES:REF:STAT OFF")        ' Disable the relative value application
        Instrument_Write(my_instrument, "ROUT:SCAN:LSEL INT")           ' Set the instrument for internal scanning
        Instrument_Write(my_instrument, "TRAC:CLE")                     ' Clear the reading buffer
        Instrument_Write(my_instrument, "TRIG:COUN 10")                 ' Set the instrument to trigger 10 times
        Instrument_Write(my_instrument, "SAMP:COUN 10")                 ' Capture 10 readings per trigger
        Instrument_Write(my_instrument, "TRAC:POIN 100")                ' Size the buffer to capture 100 total readings   
        Instrument_Write(my_instrument, "TRAC:FEED:CONT NEXT")          ' Stop filling when the buffer is full
        Instrument_Write(my_instrument, "FORM:ELEM READ, CHAN")         ' Return both readings and channel numbers
        Instrument_Write(my_instrument, "ROUT:SCAN (@1:10)")            ' Scan all ten channels of the 200x-SCAN card
        Instrument_Write(my_instrument, "STAT:MEAS:ENAB 512")           ' Enable the status register to indicate buffer full
        Instrument_Write(my_instrument, "*SRE 1")                       ' Enable the MSB bit in the status register
        Instrument_Write(my_instrument, "TRIG:SOUR EXT")                ' Respond to external trigger events
        Instrument_Write(my_instrument, "INIT")                         ' Start the scan

        Dim rcvBuffer As String = Instrument_Query(my_instrument, "STAT:MEAS?") ' Mask the measurement status to look for the buffer full condition
        Dim status_mon As Int16 = CInt(rcvBuffer)
        status_mon = status_mon And 512 ' screen for buffer full
        Do While status_mon <> 512
            Threading.Thread.Sleep(100)
            status_mon = CInt(Instrument_Query(my_instrument, "STAT:MEAS?"))
            status_mon = status_mon And 512
        Loop

        rcvBuffer = Instrument_Query(my_instrument, "TRAC:DATA?")       ' Get all the readings from the buffer
        Console.WriteLine(rcvBuffer)
        Instrument_Write(my_instrument, "ROUT:SCAN:LSEL NONE")           ' Set the instrument for internal scanning

        '
        '  Close the instrument object And release it for use
        '  by other programs.
        '
        Disconnect_From_Instrument(my_instrument)

        ' Capture the program stop time from the system.
        myStpWtch.Stop()

        ' Get the elapsed time as a TimeSpan value.
        Dim ts As TimeSpan = myStpWtch.Elapsed

        ' Format And display the TimeSpan value.
        Dim elapsedTime As String = $"{ts.Days:00}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds _
            / 10:000}"
        Console.WriteLine("RunTime " + elapsedTime)

        ' Implement a keypress capture so that the user can see the output of their program.
        Console.WriteLine("Press any key to continue...")
        Dim k As Char = Console.ReadKey().KeyChar
    End Sub

    Sub Connect_To_Instrument(ByRef resource_manager As ResourceManager, ByRef instrument_control_object As FormattedIO488, instrument_id_string As String, timeout As Int16)
        '
        '  Purpose: Open an instance Of an instrument Object For remote communication And establish the communication attributes.
        '  
        '  Parameters:
        '      resource_manager - The reference to the resource manager object created external to this function. It Is passed in 
        '                         by reference so that any internal attributes that are updated when using to connect to the 
        '                         instrument are updated to the caller. 
        '                         
        '      instrument_control_object - The reference to the instrument object created external to this function. It Is passed
        '                                  in by reference so that it retains all values upon exiting this function, making it
        '                                  consumable to all other calling functions. 
        '                                  
        '      instrument_id_string - The instrument VISA resource string used to identify the equipment at the underlying driver 
        '                             level. This string can be obtained per making a call to Find_Resources() VISA function And 
        '                             extracted from the reported list.
        '                             
        '      timeout - This Is used to define the duration of wait time that will transpire with respect to VISA read/query calls 
        '                prior to an error being reported.
        '                
        '  Returns:
        '      None
        '      
        '  Revisions: 
        '      2019-06-14      JJB     Initial revision.
        '
        instrument_control_object.IO = resource_manager.Open(instrument_id_string, Ivi.Visa.Interop.AccessMode.NO_LOCK, 20000)
        ' Instrument ID String examples...
        '       LAN -> TCPIP0:134.63.71.209:inst0:INSTR
        '       USB -> USB0:0x05E6:0x2450:01419962:INSTR
        '       GPIB -> GPIB0:16:INSTR
        '       Serial -> ASRL4:INSTR
        instrument_control_object.IO.Clear()
        Dim myTO As Int16 = instrument_control_object.IO.Timeout
        instrument_control_object.IO.Timeout = timeout
        myTO = instrument_control_object.IO.Timeout
        instrument_control_object.IO.TerminationCharacterEnabled = True
        instrument_control_object.IO.TerminationCharacter = Convert.ToByte(10)
        Return
    End Sub

    Sub Disconnect_From_Instrument(ByRef instrument_control_object As FormattedIO488)
        '
        '  Purpose: Closes an instance Of And instrument Object previously opened For remote communication.
        ' 
        '  Parameters:
        '      instrument_control_object - The reference to the instrument object created external to this function. It Is passed
        '                                  in by reference so that it retains all values upon exiting this function, making it
        '                                  consumable to all other calling functions. 
        '                
        '  Returns:
        '      None
        '      
        '  Revisions: 
        '      2019-06-14      JJB     Initial revision.
        '
        instrument_control_object.IO.Close()
        Return
    End Sub

    Sub Instrument_Write(instrument_control_object As FormattedIO488, command As String)
        '
        '  Purpose: Used to send commands to the instrument.
        '  
        '  Parameters:
        '      instrument_control_object - The reference to the instrument object created external to this function. It Is passed
        '                                  in by reference so that it retains all values upon exiting this function, making it
        '                                  consumable to all other calling functions. 
        '                                  
        '      command - The command string issued to the instrument in order to perform an action.
        '      
        '  Returns
        '      None
        '      
        '  Revisions 
        '      2019-06-04      JJB     Initial revision.
        '
        If (echo_command = True) Then
            Console.WriteLine("{0}", command)
        End If
        instrument_control_object.WriteString(command)
        Return
    End Sub

    Function Instrument_Read(instrument_control_object As FormattedIO488) As String
        '
        '  Purpose: Used to read commands from the instrument.
        '  
        '  Parameters:
        '      instrument_control_object - The reference to the instrument object created external to this function. It Is passed
        '                                  in by reference so that it retains all values upon exiting this function, making it
        '                                  consumable to all other calling functions. 
        '      
        '  Returns:
        '      The string obtained from the instrument.
        '      
        '  Revisions: 
        '      2019-06-04      JJB     Initial revision.
        '
        Return instrument_control_object.ReadString()
    End Function

    Function Instrument_Query(instrument_control_object As FormattedIO488, command As String) As String
        '
        '  Purpose: Used to send commands to the instrument  And obtain an information string from the instrument.
        '           Note that the information received will depend on the command sent And will be in string
        '           format.
        '  
        '  Parameters:
        '      instrument_control_object - The reference to the instrument object created external to this function. It Is passed
        '                                  in by reference so that it retains all values upon exiting this function, making it
        '                                  consumable to all other calling functions. 
        '                                  
        '      command - The command string issued to the instrument in order to perform an action.
        '      
        '  Returns:
        '      The string obtained from the instrument.
        '      
        '  Revisions: 
        '      2019-06-04      JJB     Initial revision.
        '
        Instrument_Write(instrument_control_object, command)
        Return Instrument_Read(instrument_control_object)
    End Function

End Module
