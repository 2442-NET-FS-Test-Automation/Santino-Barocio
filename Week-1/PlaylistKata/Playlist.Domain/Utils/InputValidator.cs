using System.Text.RegularExpressions;

namespace Playlist.Domain.Utils;

public static class InputValidator
{
    public static bool TryValidateDuration(string? input, out float validDuration)
    {
        validDuration = 0f;

        if (input is null or "")
        {
            return false;
        }

        // Regex: 1 a 3 dígitos para minutos, un formato de dos puntos ':', y 00 a 59 para segundos
        string pattern = @"^\d{1,3}:[0-5]\d$";
        
        if (Regex.IsMatch(input, pattern))
        {
            string[] parts = input.Split(':');
            int minutes = int.Parse(parts[0]);
            int seconds = int.Parse(parts[1]);

            // Convertimos a minutos con decimales 
            validDuration = minutes + (seconds / 60f);
            return true;
        }

        return false;
    }
}