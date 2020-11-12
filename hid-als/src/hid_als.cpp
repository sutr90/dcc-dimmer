/*
Copyright (c) 2014-2015 NicoHood
See the readme for credit to other people.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

#include "hid_als.h"

static const uint8_t  _hidReportDescriptorRawHID[] PROGMEM = {
  0x06, 0x89, 0xFF,            // (GLOBAL) USAGE_PAGE         0xFF89 Vendor Defined Page A 
  0x09, 0x01,                  // (LOCAL)  USAGE              0xFF890001 VendorUsage1 (Selector)  
  0xA1, 0x01,                  // (MAIN)   COLLECTION         0x01 Application (Usage=0xFF890001: Page=Vendor Defined Page A, Usage=VendorUsage1, Type=Selector)
  0x75, 0x20,                  //   (GLOBAL) REPORT_SIZE        0x10 (16) Number of bits per field  
  0x15, 0x00,                       //   (GLOBAL) LOGICAL_MINIMUM    (0)  
  0x27, 0xFF, 0xFF, 0xFF, 0x7F,            //   (GLOBAL) LOGICAL_MAXIMUM    0xFFFF (65535)  
  0x95, 0x01,                  //   (GLOBAL) REPORT_COUNT       0x01 (1) Number of fields  
  0x09, 0x01,                  //   (LOCAL)  USAGE              0xFF890001 VendorUsage1 (Selector)  
  0x81, 0x02,                  //   (MAIN)   INPUT              0x00000002 (1 fields x 16 bits) 0=Data 1=Variable 0=Absolute 0=NoWrap 0=Linear 0=PrefState 0=NoNull 0=NonVolatile 0=Bitmap 
  0x09, 0x01,                  //   (LOCAL)  USAGE              0xFF890001 VendorUsage1 (Selector)  
  0x91, 0x02,                  //   (MAIN)   OUTPUT             0x00000002 (1 fields x 16 bits) 0=Data 1=Variable 0=Absolute 0=NoWrap 0=Linear 0=PrefState 0=NoNull 0=NonVolatile 0=Bitmap 
  0xC0,                        // (MAIN)   END_COLLECTION     Application 
};



HidAls_::HidAls_(void) : PluggableUSBModule(1, 1, epType), protocol(HID_REPORT_PROTOCOL), idle(1), dataLength(0), dataAvailable(0), featureReport(NULL), featureLength(0)
{
	epType[0] = EP_TYPE_INTERRUPT_IN;
	PluggableUSB().plug(this);
}

int HidAls_::getInterface(uint8_t* interfaceCount)
{
	// Maybe as optional device FastRawHID with different USAGE PAGE
	*interfaceCount += 1; // uses 1
	HIDDescriptor hidInterface = {
		D_INTERFACE(pluggedInterface, 1, USB_DEVICE_CLASS_HUMAN_INTERFACE, HID_SUBCLASS_NONE, HID_PROTOCOL_NONE),
		D_HIDREPORT(sizeof(_hidReportDescriptorRawHID)),
		D_ENDPOINT(USB_ENDPOINT_IN(pluggedEndpoint), USB_ENDPOINT_TYPE_INTERRUPT, USB_EP_SIZE, 0x01)
	};
	return USB_SendControl(0, &hidInterface, sizeof(hidInterface));
}

int HidAls_::getDescriptor(USBSetup& setup)
{
	// Check if this is a HID Class Descriptor request
	if (setup.bmRequestType != REQUEST_DEVICETOHOST_STANDARD_INTERFACE) { return 0; }
	if (setup.wValueH != HID_REPORT_DESCRIPTOR_TYPE) { return 0; }

	// In a HID Class Descriptor wIndex cointains the interface number
	if (setup.wIndex != pluggedInterface) { return 0; }

	// Reset the protocol on reenumeration. Normally the host should not assume the state of the protocol
	// due to the USB specs, but Windows and Linux just assumes its in report mode.
	protocol = HID_REPORT_PROTOCOL;

	return USB_SendControl(TRANSFER_PGM, _hidReportDescriptorRawHID, sizeof(_hidReportDescriptorRawHID));
}

bool HidAls_::setup(USBSetup& setup)
{
	if (pluggedInterface != setup.wIndex) {
		return false;
	}

	uint8_t request = setup.bRequest;
	uint8_t requestType = setup.bmRequestType;

	if (requestType == REQUEST_DEVICETOHOST_CLASS_INTERFACE)
	{
		if (request == HID_GET_REPORT) {
			// TODO: HID_GetReport();
			return true;
		}
		if (request == HID_GET_PROTOCOL) {
			// TODO: Send8(protocol);
			return true;
		}
	}

	if (requestType == REQUEST_HOSTTODEVICE_CLASS_INTERFACE)
	{
		if (request == HID_SET_PROTOCOL) {
			protocol = setup.wValueL;
			return true;
		}
		if (request == HID_SET_IDLE) {
			idle = setup.wValueL;
			return true;
		}
		if (request == HID_SET_REPORT)
		{
			// Check if data has the correct length afterwards
			int length = setup.wLength;

			// Feature (set feature report)
			if(setup.wValueH == HID_REPORT_TYPE_FEATURE){
				// No need to check for negative featureLength values,
				// except the host tries to send more then 32k bytes.
				// We dont have that much ram anyways.
				if (length == featureLength) {
					USB_RecvControl(featureReport, featureLength);

					// Block until data is read (make length negative)
					disableFeatureReport();
					return true;
				}
			}

			// Output (set out report)
			else if(setup.wValueH == HID_REPORT_TYPE_OUTPUT){
				if(!dataAvailable && length <= dataLength){
					// Write data to fit to the end (not the beginning) of the array
					USB_RecvControl(data + dataLength - length, length);
					dataAvailable = length;
					return true;
				}
			}
		}
	}

	return false;
}

HidAls_ HidAls;
