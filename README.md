##  创建虚拟环境

```
python -m venv myenv
```

## 激活虚拟环境


```
myenv\Scripts\activate
```

## 恢复依赖文件

```
pip install -r requirements.txt
```


## 设置API_KEY

创建.env文件, 配置 密钥

```
DEEPSEEK_API_KEY = xxxx
```


### 输入测试

```
请帮我查看一下这些文件是用什么语言编写的
```

```
请帮我将这些文件重名，加上合适的扩展名
```

### 备注

resp.all_messages() 会返回历史所有对话消息，所以每次使用替换, 而不是追加

```
resp = agent.run_sync("问题1", message_history=history)
history.append(resp.all_messages())  # 这会创建嵌套列表

# ✅ 正确：替换整个历史
history = list(resp.all_messages())
```