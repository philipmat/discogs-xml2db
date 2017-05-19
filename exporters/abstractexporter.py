

class AbstractExporter(object):

    def __init__(self, params, data_quality=[]):
        self.min_data_quality = data_quality

    def finish(self, completely_done=False):
        pass

    def storeArtist(self, artist):
        pass

    def storeLabel(self, label):
        pass

    def storeRelease(self, release):
        pass

    def storeMaster(self, master):
        pass
