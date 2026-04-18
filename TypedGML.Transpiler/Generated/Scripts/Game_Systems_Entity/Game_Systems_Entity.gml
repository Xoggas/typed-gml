function Game_Systems_Entity(name, health) constructor
{
    __types = { __TYPE_Game_Systems_Entity: true, __TYPE_Game_Systems_IDestructible: true, __TYPE_Game_Systems_ILoggable: true };
    __backing_Health = undefined;
    __backing_Name = undefined;
    __backing_IsAlive = undefined;
    set_Name(name);
    set_Health(health);
    set_IsAlive(health > 0);

    static GetLog = function()
    {
        return get_Name();
    }

    static Heal = function(amount)
    {
        var next = get_Health() + amount;
        if (next > 100)
        {
            set_Health(100);
        }
        else
        {
            set_Health(next);
        }
        set_IsAlive(get_Health() > 0);
    }

    static get_Health = function() { return __backing_Health; };
    static set_Health = function(value) { __backing_Health = value; };

    static get_Name = function() { return __backing_Name; };
    static set_Name = function(value) { __backing_Name = value; };

    static get_IsAlive = function() { return __backing_IsAlive; };
    static set_IsAlive = function(value) { __backing_IsAlive = value; };
}
