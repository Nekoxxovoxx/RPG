# 动态岩浆瓦片使用说明

这套动态岩浆使用 `AnimatedHazardTile`，可以拖进 Tile Palette 后像普通瓦片一样绘制。

## 创建岩浆 Tile

1. 在 Project 面板中右键选择 `Create > Tiles > Animated Hazard Tile`。
2. 命名为 `动态岩浆瓦片` 或 `Animated Lava Tile`。
3. 在 Inspector 中设置：
   - `Frames`：按顺序拖入 `Assets/Graphics/ditu/diyuwapian/GandalfHardcore Lava Tiles` 中切好的岩浆帧。
   - `Animation Speed`：建议 `12`。
   - `Collider Type`：选择 `Grid`。
   - `Hazard Settings`：保持 Lava 默认值，或将类型设为 Lava。

## 放进 Tile Palette

1. 打开 `Window > 2D > Tile Palette`。
2. 将创建好的 `Animated Hazard Tile` 直接拖进 Tile Palette。
3. 选择这个瓦片，刷到地图上的岩浆 Tilemap。

## 伤害生效条件

岩浆必须刷在带有碰撞的 Tilemap 上才会造成伤害：

- Tilemap 对象需要有 `Tilemap Collider 2D`。
- Tilemap 对象需要有 `TilemapInstantDeathHazard`。
- 如果没有手动挂，运行时 `HazardAutoBinder` 会尝试根据名字或瓦片名自动补上。

如果岩浆只刷在纯视觉 Tilemap 上，它只会播放动画，不会伤害玩家。

## 推荐做法

- 单独建一个 `Lava_Tilemap`，专门放动态岩浆。
- 这个 Tilemap 挂 `Tilemap Collider 2D` 和 `TilemapInstantDeathHazard`。
- 地图装饰岩浆可以放在无碰撞 Tilemap；真正伤害区域放在 `Lava_Tilemap`。
