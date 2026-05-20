from pydantic import BaseModel

class TeklaRequest(BaseModel):
    message: str