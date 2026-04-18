function Game_Systems_Player(name, health, level) constructor
{
    __types = { __TYPE_Game_Systems_Player: true, __TYPE_Game_Systems_Entity: true, __TYPE_Game_Systems_IDestructible: true, __TYPE_Game_Systems_ILoggable: true };
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
    __backing_Level = undefined;
    __backing_Side = undefined;
    set_Level(level);
    set_Side(Faction.Alliance);

    static TakeDamage = function(amount)
    {
        var reduced = get_Health() - amount;
        if (reduced < 0)
        {
            set_Health(0);
            set_IsAlive(false);
        }
        else
        {
            set_Health(reduced);
            set_IsAlive(reduced > 0);
        }
    }

    static CanLevelUp = function()
    {
        return get_IsAlive() && get_Level() < 99;
    }

    static LevelUp = function()
    {
        if (CanLevelUp())
        {
            set_Level(get_Level() + 1);
            Heal(10);
        }
    }

    static get_Level = function() { return __backing_Level; };
    static set_Level = function(value) { __backing_Level = value; };

    static get_Side = function() { return __backing_Side; };
    static set_Side = function(value) { __backing_Side = value; };
}
