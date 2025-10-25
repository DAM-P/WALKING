# 🎨 Emission 材质正确配置指南

## 问题

如果材质 **Emission Color = (0, 0, 0) 黑色**，即使运行时设置了发光，方块也不会亮。

**原因**：URP 材质的 Emission Color 是**基础色调**，运行时的 `URPMaterialPropertyEmissionColor` 会与之混合。

---

## ✅ 正确配置

### Cube Prefab Material 设置

**位置**：Cube Prefab > Material > Emission

| 属性 | 正确值 | 说明 |
|------|--------|------|
| `Emission` 复选框 | ☑ 勾选 | 必须启用 |
| `Emission Color/Map` | **白色 (1, 1, 1, 1)** | 基础色调，白色表示不改变颜色 |
| `HDR 强度滑块` | **0** 或任意 | 材质默认强度（实例会覆盖） |

**关键**：
- ✅ **Emission Color = 白色** → 允许运行时完全控制颜色
- ✅ **HDR 强度 = 0** → 材质本身不发光（或者任意，因为实例会覆盖）

---

## 🎨 原理说明

### URP Entities Graphics 工作机制

```
最终颜色 = 材质 Emission Color × 实例 URPMaterialPropertyEmissionColor
```

**示例**：

#### 配置 A：材质黑色（错误）❌
```
材质：Emission Color = (0, 0, 0)
运行时：URPMaterialPropertyEmissionColor = (10, 10, 0) 黄色
结果：(0, 0, 0) × (10, 10, 0) = (0, 0, 0) 黑色 → 不发光
```

#### 配置 B：材质白色（正确）✅
```
材质：Emission Color = (1, 1, 1)
运行时未选中：URPMaterialPropertyEmissionColor = (0, 0, 0)
结果：(1, 1, 1) × (0, 0, 0) = (0, 0, 0) 黑色 → 不发光

材质：Emission Color = (1, 1, 1)
运行时选中：URPMaterialPropertyEmissionColor = (1.2, 1.2, 0)
结果：(1, 1, 1) × (1.2, 1.2, 0) = (1.2, 1.2, 0) 黄色 → 发光！
```

---

## 🚀 配置步骤

### 步骤1：设置材质 Emission

1. **Project 窗口**找到 Cube 的 **Material**
2. **选中 Material**，查看 Inspector
3. **找到 Emission 区块**
4. **配置如下**：
   - ☑ 勾选 `Emission` 复选框
   - 点击 **Emission Color 色块**
   - 颜色选择器设置为 **白色 (255, 255, 255)** 或 RGB **(1, 1, 1)**
   - **HDR 强度滑块**拖到 **0**（或保持默认，实例会覆盖）
5. **保存 Material**（Ctrl+S）

### 步骤2：验证配置

使用 **Cube Material Checker**：

1. Hierarchy 创建空 GameObject
2. Add Component → `Cube Material Checker`
3. 拖入 Cube Prefab
4. 右键组件 → `Check Material`

**应该看到**：
```
========== Cube 材质检查 ==========
✅ Shader 是 URP Shader
✅ Material 启用了 Emission Keyword
✅ Material 有 _EmissionColor 属性
Emission Color: RGBA(1.000, 1.000, 1.000, 1.000)  ← 白色
========== 检查完成 ==========
```

### 步骤3：测试

1. **完全退出 Play 模式**
2. **重新进入 Play**
3. **点击方块**
4. **应该看到**：
   - 常态：不发光
   - 选中：黄色脉冲发光
   - 取消：淡出

---

## 🎨 高级配置

### 配置1：基础色调（推荐）

**适用**：标准高亮效果

| 材质设置 | 值 |
|---------|---|
| Emission Color | 白色 (1, 1, 1) |
| HDR 强度 | 0 |

**效果**：运行时完全控制颜色

---

### 配置2：预设色调

**适用**：所有可交互方块统一颜色（如全部黄色高亮）

| 材质设置 | 值 |
|---------|---|
| Emission Color | 黄色 (1, 1, 0) |
| HDR 强度 | 0 |

**代码调整**：运行时只需设置强度
```csharp
// HighlightRenderSystem 中
emission.ValueRW.Value = new float4(1, 1, 1, 1) * intensity; // 白色 × 强度
```

**效果**：材质色调 × 白色强度 = 黄色发光

---

### 配置3：半透明基础色

**适用**：让高亮更柔和

| 材质设置 | 值 |
|---------|---|
| Emission Color | 浅灰 (0.5, 0.5, 0.5) |
| HDR 强度 | 0 |

**效果**：高亮颜色会变暗 50%，更柔和

---

## 🔍 故障排查

### ❌ 设置白色后，所有 Cube 都在发光

**原因**：材质 HDR 强度 > 0

**解决**：
- HDR 强度滑块拖到 **最左（0）**
- 或者检查 `CubeLayoutSpawnerAuthoring.EmissionIntensity = 0`

---

### ❌ 设置白色后，选中方块还是不发光

**原因1**：运行时没有设置 `URPMaterialPropertyEmissionColor`

**检查**：
点击方块后，Console 应显示：
```
[Selection] ✅ Entity 有 Emission 组件
```

**原因2**：`HighlightRenderSystem` 未运行

**检查**：
- Window > Entities > Systems
- 搜索 `HighlightRenderSystem`
- `Running = True`？

---

### ❌ 高亮颜色不对（如蓝色变绿色）

**原因**：材质 Emission Color 不是白色

**解决**：
- 设置为 **纯白 (1, 1, 1)**
- 不要用其他颜色（除非想要色调偏移）

---

## 📊 不同材质颜色的效果对比

| 材质 Emission Color | 运行时颜色 | 最终效果 |
|-------------------|-----------|---------|
| (0, 0, 0) 黑色 | (1, 1, 0) 黄色 | **(0, 0, 0) 不发光** ❌ |
| (1, 1, 1) 白色 | (1, 1, 0) 黄色 | **(1, 1, 0) 黄色发光** ✅ |
| (1, 0, 0) 红色 | (1, 1, 0) 黄色 | **(1, 0, 0) 红色发光** ⚠️ 颜色被改变 |
| (0.5, 0.5, 0.5) 灰 | (2, 2, 0) 黄色 | **(1, 1, 0) 柔和黄色** ✅ 可用 |

---

## 💡 最佳实践

### 推荐配置（标准）

**Material**：
- Emission Color = **(1, 1, 1, 1)** 白色
- HDR 强度 = **0**

**CubeLayoutSpawnerAuthoring**：
- Emission Intensity = **0**

**CubeSelectionManager**：
- Highlight Color = **(1, 1, 0)** 黄色
- Highlight Intensity = **1.2**

**效果**：
- 常态：不发光
- 选中：黄色脉冲（强度 0.6 ~ 1.0）

---

### 可选配置（柔和）

**Material**：
- Emission Color = **(0.5, 0.5, 0.5, 1)** 灰色
- HDR 强度 = **0**

**代码调整**：
```csharp
// CubeSelectionManager
highlightIntensity = 2.4f; // 补偿灰色的暗化
```

**效果**：更柔和的高亮

---

## 🎯 快速修复

如果你的材质 Emission Color 是黑色：

### 方法1：手动修改（推荐）

1. 选中 Material
2. Emission Color → **白色 (1, 1, 1)**
3. HDR 强度 → **0**
4. 保存

### 方法2：使用 Auto Fix（快速）

1. GameObject 添加 `Cube Material Checker`
2. 拖入 Cube Prefab
3. 右键组件 → `Auto Fix Material (Attempt)`
4. Ctrl+S 保存

---

## 📝 总结

| 项目 | 错误配置 | 正确配置 |
|------|---------|---------|
| Emission Color | (0, 0, 0) 黑色 | **(1, 1, 1) 白色** |
| HDR 强度 | > 0 | **0** |
| Emission 复选框 | ☐ 未勾选 | **☑ 勾选** |

**核心原则**：
- ✅ 材质提供**基础色调**（白色 = 中性）
- ✅ 运行时控制**实际颜色和强度**
- ✅ HDR 强度为 0 避免材质自发光

---

祝配置成功！现在选中方块应该能正常发光了。🔥


