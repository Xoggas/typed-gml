function Game_Systems_Weapon() constructor
{
    __types = { __TYPE_Game_Systems_Weapon: true };
    __backing_Name = undefined;
    __backing_Damage = undefined;
    if (argument_count == 1)
    {
        var name = argument[0];
        set_Name(name);
        set_Damage(10);
    }
    else
    {
        var name = argument[0];
        var damage = argument[1];
        set_Name(name);
        set_Damage(damage);
    }

    static Scale_0 = function(factor)
    {
        return get_Damage() * factor;
    }

    static Scale_1 = function(prefix)
    {
        return prefix;
    }

    static Scale = function()
    {
        if (argument_count == 1 && is_string(argument[0])) { return Scale_1(argument[0]); }
        else { return Scale_0(argument[0]); }
    }

    static get_Name = function() { return __backing_Name; };
    static set_Name = function(value) { __backing_Name = value; };

    static get_Damage = function() { return __backing_Damage; };
    static set_Damage = function(value) { __backing_Damage = value; };
}
