﻿namespace Imgeneus.DatabaseBackgroundService.Handlers
{
    /// <summary>
    /// All possible actions to database.
    /// </summary>
    public enum ActionType
    {
        // Character
        SAVE_CHARACTER_MOVE,
        SAVE_CHARACTER_HP_MP_SP,

        // Inventory
        SAVE_ITEM_IN_INVENTORY,
        REMOVE_ITEM_FROM_INVENTORY,
        UPDATE_ITEM_COUNT_IN_INVENTORY,
        UPDATE_GOLD,

        // Skills
        SAVE_SKILL,
        REMOVE_SKILL,

        // Buffs
        SAVE_BUFF,
        REMOVE_BUFF,
        REMOVE_BUFF_ALL,
        UPDATE_BUFF_RESET_TIME,

        // Quests
        QUEST_START,
        QUEST_UPDATE,

        // Logs
        LOG_SAVE_CHAT_MESSAGE
    }
}
