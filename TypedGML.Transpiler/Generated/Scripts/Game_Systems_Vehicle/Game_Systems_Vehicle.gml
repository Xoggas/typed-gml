function Game_Systems_Vehicle() constructor
{
    __types = { __TYPE_Game_Systems_Vehicle: true };
    __backing_Model = undefined;
    __backing_Speed = undefined;
    if (argument_count == 1)
    {
        var model = argument[0];
        set_Model(model);
        set_Speed(60);
    }
    else
    {
        var model = argument[0];
        var speed = argument[1];
        set_Model(model);
        set_Speed(speed);
    }

    static get_Model = function() { return __backing_Model; };
    static set_Model = function(value) { __backing_Model = value; };

    static get_Speed = function() { return __backing_Speed; };
    static set_Speed = function(value) { __backing_Speed = value; };
}
