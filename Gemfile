source "https://rubygems.org"
# iOS builds use Unity's Xcode project directly; no CocoaPods dependency is needed.
gem "fastlane"
gem "mutex_m"
gem "abbrev"
plugins_path = File.join(File.dirname(__FILE__), 'fastlane', 'Pluginfile')
eval_gemfile("fastlane/Pluginfile") if File.exist?(plugins_path)
