function Game_Systems_Car() constructor
{
    __types = { __TYPE_Game_Systems_Car: true, __TYPE_Game_Systems_Vehicle: true };
    __backing_Model = undefined;
    __backing_Speed = undefined;

    static get_Model = function() { return __backing_Model; };
    static set_Model = function(value) { __backing_Model = value; };

    static get_Speed = function() { return __backing_Speed; };
    static set_Speed = function(value) { __backing_Speed = value; };
    __backing_Doors = undefined;
    if (argument_count == 1)
    {
        var model = argument[0];
        set_Model(model);
        set_Speed(60);
        set_Doors(4);
    }
    else
    {
        var model = argument[0];
        var speed = argument[1];
        var doors = argument[2];
        set_Model(model);
        set_Speed(speed);
        set_Doors(doors);
    }

    static get_Doors = function() { return __backing_Doors; };
    static set_Doors = function(value) { __backing_Doors = value; };
}
