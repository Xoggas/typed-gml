function Game_Systems_Logger() constructor
{
    __types = { __TYPE_Game_Systems_Logger: true };

    static Log_0 = function(code)
    {
        var x = code;
    }

    static Log_1 = function(message)
    {
        var s = message;
    }

    static Log_2 = function(player)
    {
        var s = player.Name;
    }

    static Log = function()
    {
        if (argument_count == 1 && is_string(argument[0])) { Log_1(argument[0]); }
        else if (argument_count == 1 && is_struct(argument[0])) { Log_2(argument[0]); }
        else { Log_0(argument[0]); }
    }
}
