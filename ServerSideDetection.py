
# Import FastAPI
import uvicorn
from fastapi import FastAPI, File, UploadFile
from fastapi.responses import FileResponse
from fastapi.responses import StreamingResponse
from PIL import Image, ImageOps, ImageDraw, ImageFont
import matplotlib.pyplot as plt
import numpy as np
import io
import subprocess
import torch
# from yolov5 import YOLOv5



model = torch.hub.load('ultralytics/yolov5', 'yolov5l', pretrained=True)

# Create an instance of the FastAPI class
app = FastAPI()

# Define a route: A GET request to the URL "/" will be handled by this function
@app.get("/hello")
async def read_root():
    # This function returns a JSON response with a key "message"
    return {"message": "Hello World"}

# If you want to add more endpoints, you can define more functions here.
# For example, a goodbye message:
@app.get("/goodbye")
async def read_goodbye():
    return {"message": "Goodbye World"}


@app.get("/gpu-status")
def get_gpu_status():
    # Execute the nvidia-smi command
    result = subprocess.run(["nvidia-smi"], capture_output=True, text=True)

    # Return the output or error
    if result.returncode == 0:
        return str(result.stdout)
    else:
        return {"error": result.stderr}

@app.post("/invert-color")
async def invert_color(file: UploadFile = File(...)):
    # return {"message": "File received"}

    # Read image file
    contents = await file.read()
    image = Image.open(io.BytesIO(contents))
    print(type(image))
    # save image
    image.save("test.png")
    print("image saved")

    # Invert colors
    image = image.convert("RGB")  # Convert image to RGB mode, otherwise it will fail
    inverted_image = ImageOps.invert(image)
    print('inverted image')

    # Save the processed image to a bytes buffer
    img_byte_arr = io.BytesIO()
    inverted_image.save(img_byte_arr, format='PNG')
    img_byte_arr.seek(0)

    return StreamingResponse(img_byte_arr, media_type="image/png")

@app.post("/detect-objects")
async def detect_objects(file: UploadFile = File(...)):
    contents = await file.read()
    # for phones, counter clockwise 90 degree

    img = Image.open(io.BytesIO(contents)).convert("RGB")
    img = ImageOps.exif_transpose(img)
    img = img.rotate(-90, expand=True)

    # img = img.resize((640, 960), Image.Resampling.LANCZOS)
    results = model(img)
    rendered_img = results.render()

    pil_img = Image.fromarray(rendered_img[0])
    pil_img = pil_img.rotate(90, expand=True)

    # Save the processed image to a BytesIO buffer
    img_byte_arr = io.BytesIO()
    pil_img.save(img_byte_arr, format='PNG')
    img_byte_arr.seek(0)  # Reset buffer pointer to the beginning

    return StreamingResponse(io.BytesIO(img_byte_arr.getvalue()), media_type="image/png")




# uvicorn main:app --reload
# Run the API with uvicorn
if __name__ == '__main__':
    # server address: 10.237.198.206
    # uvicorn.run(app="test:app", host='172.18.0.1', port=5958) # not ok (cannot be accessed from outside)
    uvicorn.run(app="test:app", host='10.237.198.206', port=5958) # ok
    # uvicorn.run(app="test:app", host='0.0.0.0', port=5958) # ok
    # uvicorn.run(app="test:app", host='10.237.198.205', port=5958) # not ok (cannot assign requested address)