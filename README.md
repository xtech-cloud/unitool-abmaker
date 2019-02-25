# 使用教程

## 打开项目

- 使用Unity打开ABMaker工程。

## 创建资源组文件夹

- 在Assets/Packages目录下创建一个文件夹，用于区别资源分组，例如model.character。

## 创建资源包
- 右键单机资源组文件夹，例如model.character。
- 在弹出的菜单中选择"New Asset",会自动生成一个uuid的文件夹，例如d3978ab959294e2c87afe284c6f4612c。
- 在自动创建的uuid命名的文件夹中，会自动生成一个_manifest.asset文件，此文件用于保存资源包的相关信息，其中只有Alias字段需要手动填写，Alias表示资源包或资源文件的别名，其余字段会自动更新。
- 将原始素材放到Assets/Source文件夹下， 此处使用Unity官方素材Robot Kyle。
- 将需要打包到AssetBundle的文件放到uuid形式的资源文件夹中。例如robot.prefab。(Unity打包AssetBundle时会自动处理引用关系，所以其他被引用到的文件可以放在Source目录中不予理会)。例如此处打包prefab文件，prefab用到的fbx、mat、tag等文件依然在Source/Robot Kyle中。
- 使用菜单栏 BuildTools/Refresh 刷新数据，此操作将填充_manifest.asset文件，并为资源文件夹中的所有除_manifest.asset的其他文件设置AssetBundleName（只有设置了AssetBundleName的文件才会被打包到AssetBundle中）。

## 构建资源包
- 首先执行BuildTools/Refresh进行一次刷新。
- 执行BuildTools/Manifest/Export导出资源清单文件，输出目录为和ABMaker文件夹同级的_assets/meta。
- 执行BuildTools/AssetBundle/Win32构建windows平台的assetbundle，输出目录为和ABMaker同级的_assets/win32。

# 扩展教程

## 直接在Unity素材包中创建资源的方法

- 导入Unity素材包后，将文件夹移到Source目录。
- 选中需要处理的文件，一般情况下是prefab文件，也可以是音频文件和材质文件或者图片等。
- 在选中的文件上执行右键菜单中的"Process",完成后会在Assets/_out目录得到很多uuid命名的文件，将这些文件夹移动到对应的资源组文件夹中。
- 执行BuildTools/Refresh刷新数据。

## 导出场景

- 打开一个场景文件。
- 先将场景中使用到的prefab通过process处理为资源文件。
- 执行BuildTools/Scene/Export，此操作将会将场景中所有用到prefab的对象输出到_asset/scene/scene.json文件中。

