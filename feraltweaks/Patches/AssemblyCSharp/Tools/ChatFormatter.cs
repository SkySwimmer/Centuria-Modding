using System;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class ChatFormatter
    {
        public static string Format(string message, bool useNoParseTrick = false)
        {
            // Build new message
            message = FormatInternalRecurse(message, useNoParseTrick);

            // Process escaping and quick emoji
            int i = 0;
            int skip = 0;
            bool escaped = false;
            bool lastEscaped = false;
            string newMessage = "";
            // TODO: quick emoji
            foreach (char ch in message)
            {
                if (skip > 0)
                {
                    skip--;
                    i++;
                    continue;
                }

                // Check escape
                if (escaped && ch != '\\' && !lastEscaped)
                    escaped = false;
                if (ch == '\\' && !escaped)
                    escaped = true;
                else
                    lastEscaped = false;

                // Add to message
                if (ch != '\\' || lastEscaped)
                {
                    newMessage += ch;
                }
                if (escaped)
                    lastEscaped = true;
                i++;
            }

            // Return
            return newMessage;
        }

        private static string FormatInternalRecurse(string message, bool useNoParseTrick)
        {
            int i = 0;
            int skip = 0;
            bool control = false;
            bool escaped = false; // \
            bool italic = false; // *
            bool strikethrough = false; // ~~
            bool underlined = false; // __
            bool bold = false; // **
            bool emoji = false; // :
            string controlBufferBold = "";
            string controlBufferItalic = "";
            string controlBufferUnderline = "";
            string controlBufferStrikethrough = "";
            string newMessage = "";
            bool lastEscaped = false;
            foreach (char ch in message)
            {
                if (skip > 0)
                {
                    skip--;
                    i++;
                    continue;
                }

                // Check escape
                if (escaped && ch != '\\' && !lastEscaped)
                    escaped = false;
                if (ch == '\\' && !escaped)
                {
                    escaped = true;
                    lastEscaped = true;
                }
                else
                    lastEscaped = false;

                // Check control character
                if (ch == '*' && !escaped && (!control || bold || italic))
                {
                    // Check buffer
                    if (controlBufferItalic != "" || controlBufferBold != "")
                    {
                        // Check mode
                        bool valid = false;
                        bool wasBold = bold;
                        bool wasItalic = italic;
                        if (i + 1 < message.Length && message[i + 1] == '*' && bold && (i + 2 >= message.Length || message[i + 2] != '*'))
                        {
                            // 2 stars
                            bold = false;
                            valid = true;
                            skip += 1;
                        }
                        else if (italic && (i + 1 >= message.Length || message[i + 1] != '*'))
                        {
                            // 1 star
                            italic = false;
                            valid = true;
                        }
                        if (!valid)
                        {
                            // Add to buffer
                            if (bold)
                                controlBufferBold += ch;
                            else if (italic)
                                controlBufferItalic += ch;
                            else if (underlined)
                                controlBufferUnderline += ch;
                            else if (strikethrough)
                                controlBufferStrikethrough += ch;
                        }
                        else
                        {
                            // End of buffer, add formatted text
                            string buff = "";
                            if (useNoParseTrick)
                                buff += "</noparse>";
                            if (wasItalic)
                                buff += "<i>";
                            if (wasBold)
                                buff += "<b>";
                            if (useNoParseTrick)
                                buff += "<noparse>";
                            if (wasBold)
                                buff += FormatInternalRecurse(controlBufferBold, useNoParseTrick);
                            if (wasItalic)
                                buff += FormatInternalRecurse(controlBufferItalic, useNoParseTrick);
                            if (useNoParseTrick)
                                buff += "</noparse>";
                            if (wasItalic)
                                buff += "</i>";
                            if (wasBold)
                                buff += "</b>";
                            if (useNoParseTrick)
                                buff += "<noparse>";

                            // Add
                            if (bold)
                                controlBufferBold += buff;
                            else if (italic)
                                controlBufferItalic += buff;
                            else if (underlined)
                                controlBufferUnderline += buff;
                            else if (strikethrough)
                                controlBufferStrikethrough += buff;
                            else
                                newMessage += buff;

                            // Clear buffer
                            controlBufferBold = "";
                            controlBufferItalic = "";
                            control = false;
                        }
                    }
                    else
                    {
                        // Control request
                        if (!italic && !bold)
                        {
                            // Single star
                            italic = true;
                            control = true;
                        }
                        else if (!bold)
                        {
                            // Two stars, bold
                            italic = false;
                            bold = true;
                            control = true;
                        }
                        else
                        {
                            // Invalid
                            if (bold)
                                controlBufferBold += ch;
                            else if (italic)
                                controlBufferItalic += ch;
                            else if (underlined)
                                controlBufferUnderline += ch;
                            else if (strikethrough)
                                controlBufferStrikethrough += ch;
                            else
                                newMessage += ch;
                        }
                    }
                }
                else if (ch == '_' && !escaped && (!control || underlined) && i + 1 < message.Length && message[i + 1] == '_')
                {
                    // Skip next underscore
                    skip++;

                    // Check buffer
                    if (controlBufferUnderline != "")
                    {
                        // Mark done
                        underlined = false;

                        // End of buffer, add formatted text
                        string buff = "";
                        if (useNoParseTrick)
                            buff += "</noparse>";
                        buff += "<u>";
                        if (useNoParseTrick)
                            buff += "<noparse>";
                        buff += FormatInternalRecurse(controlBufferUnderline, useNoParseTrick);
                        if (useNoParseTrick)
                            buff += "</noparse>";
                        buff += "</u>";
                        if (useNoParseTrick)
                            buff += "<noparse>";

                        // Add
                        if (bold)
                            controlBufferBold += buff;
                        else if (italic)
                            controlBufferItalic += buff;
                        else if (underlined)
                            controlBufferUnderline += buff;
                        else if (strikethrough)
                            controlBufferStrikethrough += buff;
                        else
                            newMessage += buff;

                        // Clear buffer
                        controlBufferUnderline = "";
                        control = false;
                    }
                    else
                    {
                        // Control request
                        if (!underlined)
                        {
                            underlined = true;
                            control = true;
                        }
                        else
                        {
                            // Invalid
                            if (bold)
                            {
                                controlBufferBold += ch;
                                controlBufferBold += ch;
                            }
                            else if (italic)
                            {
                                controlBufferItalic += ch;
                                controlBufferItalic += ch;
                            }
                            else if (underlined)
                            {
                                controlBufferUnderline += ch;
                                controlBufferUnderline += ch;
                            }
                            else if (strikethrough)
                            {
                                controlBufferStrikethrough += ch;
                                controlBufferStrikethrough += ch;
                            }
                            else
                            {
                                newMessage += ch;
                                newMessage += ch;
                            }
                        }
                    }
                }
                else if (ch == '~' && !escaped && (!control || strikethrough) && i + 1 < message.Length && message[i + 1] == '~')
                {
                    // Skip next tilde
                    skip++;

                    // Check buffer
                    if (controlBufferStrikethrough != "")
                    {
                        // Mark done
                        strikethrough = false;

                        // End of buffer, add formatted text
                        string buff = "";
                        if (useNoParseTrick)
                            buff += "</noparse>";
                        buff += "<s>";
                        if (useNoParseTrick)
                            buff += "<noparse>";
                        buff += FormatInternalRecurse(controlBufferStrikethrough, useNoParseTrick);
                        if (useNoParseTrick)
                            buff += "</noparse>";
                        buff += "</s>";
                        if (useNoParseTrick)
                            buff += "<noparse>";

                        // Add
                        if (bold)
                            controlBufferBold += buff;
                        else if (italic)
                            controlBufferItalic += buff;
                        else if (underlined)
                            controlBufferUnderline += buff;
                        else if (strikethrough)
                            controlBufferStrikethrough += buff;
                        else
                            newMessage += buff;

                        // Clear buffer
                        controlBufferStrikethrough = "";
                        control = false;
                    }
                    else
                    {
                        // Control request
                        if (!strikethrough)
                        {
                            strikethrough = true;
                            control = true;
                        }
                        else
                        {
                            // Invalid
                            if (bold)
                            {
                                controlBufferBold += ch;
                                controlBufferBold += ch;
                            }
                            else if (italic)
                            {
                                controlBufferItalic += ch;
                                controlBufferItalic += ch;
                            }
                            else if (underlined)
                            {
                                controlBufferUnderline += ch;
                                controlBufferUnderline += ch;
                            }
                            else if (strikethrough)
                            {
                                controlBufferStrikethrough += ch;
                                controlBufferStrikethrough += ch;
                            }
                            else
                            {
                                newMessage += ch;
                                newMessage += ch;
                            }
                        }
                    }
                }
                else
                {
                    // Fallback
                    if (bold)
                        controlBufferBold += ch;
                    else if (italic)
                        controlBufferItalic += ch;
                    else if (underlined)
                        controlBufferUnderline += ch;
                    else if (strikethrough)
                        controlBufferStrikethrough += ch;
                    else
                        newMessage += ch;
                }

                i++;
            }

            // Wrap up
            if (italic)
                newMessage += "*";
            if (bold)
                newMessage += "**";
            if (underlined)
                newMessage += "__";
            if (strikethrough)
                newMessage += "~~";
            if (italic)
                newMessage += controlBufferItalic;
            if (bold)
                newMessage += controlBufferBold;
            if (underlined)
                newMessage += controlBufferUnderline;
            if (strikethrough)
                newMessage += controlBufferStrikethrough;

            // Return
            return newMessage;
        }
    }
}