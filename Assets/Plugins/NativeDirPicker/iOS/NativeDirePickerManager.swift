import Foundation

@objc public class OtaNativeDirPickerManager : NSObject, UIDocumentPickerDelegate {
    static var dirPickerManager: OtaNativeDirPickerManager?
    var viewController: UIViewController?
    var url: URL?
    var bytes: Data?
    
    @objc public static func getInstance() -> OtaNativeDirPickerManager {
        if dirPickerManager == nil {
            dirPickerManager = OtaNativeDirPickerManager()
        }
        return dirPickerManager!
    }

    @objc public func pickDirWithSecurityScope(controller: UIViewController) {
        self.viewController = controller
        let picker = UIDocumentPickerViewController(forOpeningContentTypes: [.folder], asCopy: false)
        picker.delegate = self
        controller.present(picker, animated: true, completion: nil)
    }
    
    @objc public func getSecurityScopeURL() -> String {
        if url != nil {
            return self.url!.absoluteString
        } else {
            return ""
        }
    }

    @objc public func getSecurityScopeBookmark() -> NSData? {
        if url != nil {
            return NSData(data: self.bytes!)
        } else {
            return nil
        }
    }
    
    @objc public func stopAccessingSecurityScopedResource() {
        if url != nil {
            self.url?.stopAccessingSecurityScopedResource()
        } else {
            print("Directory has not been selected!!")
        }
    }

    public func documentPicker(_ controller: UIDocumentPickerViewController, didPickDocumentAt url: URL) {
        do {
            print("Succssfully have picked")
            // URLを取得
            //guard let url = urls.first else { return }
            print(url.absoluteString)
            guard url.startAccessingSecurityScopedResource() else {
                return
            }
            self.url = url
            print("start accessing security scoped resource!!")
            let bookmarkData = try url.bookmarkData(options: .minimalBookmark, includingResourceValuesForKeys: nil, relativeTo: nil)
            self.bytes = bookmarkData
            return
        } catch let error {
            print("Error occured: \(error)")
            self.url = nil
        }
        defer { url.stopAccessingSecurityScopedResource() }
    }
    
}
